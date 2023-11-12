module Program

open System

[<EntryPoint>]
let main argv =
    if Array.length argv = 2 then
        let programType = argv.[0]
        let port = argv.[1]

        try
            let portNum = Int32.Parse port

            if programType = "server" then
                Server.startServer portNum // start the server on port number
                0
            elif programType = "client" then
                Client.runClient portNum // connect the client to the server (port number)
                0
            else
                printfn "Invalid programType. Use 'server' or 'client'."
                1
        with :? FormatException ->
            printfn "Invalid port number."
            1
    else
        printfn "Usage: dotnet run -- (server|client) <port>"
        1
