open System
open System.IO
open System.Net
open System.Text
open System.Net.Sockets
open System.Threading.Tasks
open System.Collections.Generic


// Unique Id for each client that is connected to the server
let mutable clientId = 0

// Dictionary that is used to map client port numbers to the Client Id
let portClientDictionary = new Dictionary<int, int>()

let mutable serverTerminationFlag = false

(*  
    This mapping of port of client & client id is required to 
    print the client number for each operation that is executed for a specific client.
*)
let mapClientPortToClientId (clientNumber: int) (portNumber: int) =
    portClientDictionary.Add(clientNumber, portNumber)



(*  Function that performs arithmatic operations like add, subtract, multiply
    Number format exception is handled for invalid operands. 
*)
let performArithmeticOperations (inputArray: string[]) =
    let arithmeticOperator = inputArray.[0]
    let operands = inputArray.[1..]

    // check if all operands are non-negative natural numbers
    let areOperandsValid (operands: string[]) =
        Array.forall
            (fun (x: string) ->
                match Int32.TryParse(x) with
                | (true, value) when value > 0 -> true // Check if it's a positive integer
                | _ -> false)
            operands

    // covert the string operands to integer operands
    let integerOperands =
        try
            Array.tail inputArray |> Array.map Int32.Parse
        with :? FormatException -> // Handle number-format-exception
            [| -4 |]

    // Exception Hnadling for invalid operands
    if
        arithmeticOperator <> "add"
        && arithmeticOperator <> "subtract"
        && arithmeticOperator <> "multiply"
    then
        [| -1 |] // invalid arithmetic operator
    elif Array.length operands < 2 then
        [| -2 |] // < 2 operands for an arithmetic operation
    elif Array.length operands > 4 then
        [| -3 |] // > 4 operands for an arithmetic operation
    elif not (areOperandsValid operands) then
        [| -4 |] // invalid operands.
    else
        try
            match arithmeticOperator with
            | "add" -> [| Array.sum integerOperands |]
            | "subtract" -> [| integerOperands.[0] - Array.sum (Array.tail integerOperands) |]
            | "multiply" -> [| Array.fold (*) integerOperands.[0] (Array.tail integerOperands) |]
            | _ -> [| -1 |]
        with :? FormatException ->
            [| -4 |]



(*  Function to handle all operations for the client. Operations include 
    1. read the input from the client
    2. Perform Arithmetic Operation
    3. write the output to the client
*)
let handleClientRequests (client: TcpClient) =
    async {
        try
            // Initialize streams to read & write messages to and from client
            use streamWriter = new System.IO.StreamWriter(client.GetStream(), Encoding.ASCII)
            use streamReader = new System.IO.StreamReader(client.GetStream())


            // write the output to the client
            let sendResponse (response: string) =
                streamWriter.WriteLine(response)
                streamWriter.Flush()

            let endPoint = client.Client.RemoteEndPoint :?> IPEndPoint
            clientId <- clientId + 1
            mapClientPortToClientId endPoint.Port clientId

            // send initial hello message to the client.
            sendResponse ("Hello!")

            // Flag to check if client is terminated.
            let mutable isClientConnected = true

            // A specific client will continue to run until bye or temination is issued.
            while isClientConnected && not serverTerminationFlag do
                let message = streamReader.ReadLine()
                printfn "Received: %s" message
                let parts = message.Split(' ')

                // Terminate a specific client
                if parts[0] = "bye" then
                    isClientConnected <- false
                    printfn "Responding to client %d with result: %s" portClientDictionary.[endPoint.Port] "-5"
                    sendResponse ("-5")
                //Terminate all the clients and then the server
                elif parts[0] = "terminate" then
                    serverTerminationFlag <- true
                    printfn "Responding to client %d with result: %s" portClientDictionary.[endPoint.Port] ("-5")
                    sendResponse ("-5")
                else
                    let results = performArithmeticOperations parts

                    printfn
                        "Responding to client %d with result: %s"
                        portClientDictionary.[endPoint.Port]
                        (results[0].ToString())

                    sendResponse (results.[0].ToString())
        with
        | :? SocketException as ex -> printfn "SocketException: %s" ex.Message // handle socket exceptions like null
        | :? IOException as ex -> printfn "IOException: %s" ex.Message
        | ex -> printfn "An error occurred: %s" ex.Message
    }


// Main Block
let startServer (port) =
    let listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port)
    listener.Start()
    printfn "Server is running and listening on port %d" port

    // Stores the list of all clients (includes client port number, socket etc..)
    let clientTasks = new List<Task>()

    // Listen to multiple client until termination is issued.
    while not serverTerminationFlag do
        if listener.Pending() then
            let client = listener.AcceptTcpClient()
            let clientTask = Task.Run(fun () -> Async.Start(handleClientRequests client))
            clientTasks.Add(clientTask)

    Task.WaitAll(clientTasks.ToArray())
    listener.Stop()
