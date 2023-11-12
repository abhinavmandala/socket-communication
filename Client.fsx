open System
open System.IO
open System.Net.Sockets
open System.Threading.Tasks

let runClient port =
    use client = new TcpClient()

    try
        client.Connect("127.0.0.1", port)
    with
    | :? System.Net.Sockets.SocketException as ex ->
        printf "Server is not connected"
        Environment.Exit(0)
    | :? System.InvalidOperationException as ex ->
        printf "Server is not connected"
        Environment.Exit(0)

    use stream = client.GetStream()
    use reader = new StreamReader(stream)
    use writer = new StreamWriter(stream)
    writer.AutoFlush <- true

    // Fag to check if client is terminated (false) or still running (true)
    let mutable isClientActive = true

    let closeConnection () =
        client.Close()
        reader.Close()
        writer.Close()
        stream.Close()

    let readFromServerAsync () =
        Task.Run(fun () ->
            try
                while isClientActive do
                    let mutable response = reader.ReadLine()

                    // Handle exceptions
                    if response = "-4" then
                        response <- "one or more of the inputs contain(s) non-number(s)."
                    elif response = "-3" then
                        response <- "number of inputs is more than four."
                    elif response = "-2" then
                        response <- "number of inputs is less than two."
                    elif response = "-1" then
                        response <- "incorrect operation command."


                    if response = "-5" || response = null then
                        printf "exit"
                        isClientActive <- false
                        // Exit the client
                        closeConnection ()
                        Environment.Exit(0)
                    else
                        printfn "Server response: %s" response
                        printf "Sending command: "
            with :? IOException ->
                printfn ""
                printfn "exit"
                isClientActive <- false
                closeConnection ()
                Environment.Exit(0))

    let writeToServerAsync () =
        Task.Run(fun () ->
            // Write commands to the server
            while isClientActive do
                let input = Console.ReadLine()

                if not (System.String.IsNullOrEmpty(input)) then
                    writer.WriteLine(input))

    // Function to read the data from the server
    let serverReadTask = readFromServerAsync ()
    // Fucntion to write the data to the server
    let serverWriteTask = writeToServerAsync ()

    Task.WaitAll([| serverReadTask; serverWriteTask |])
