open System
open System.Configuration
open System.Diagnostics
open System.IO

[<EntryPoint>]
let main argv = 
    // Basic Information
    let showUsage = "\nUsage:\n\nBruteForceCracker.exe {0} {1} {2} {3}\n\n{0} - File to be unzipped\n{1} - Minimum password length\n{2} - Maximum password length\n{3} - Attack type: 'b' for 'Brute Force', 'd' for 'Dictionary'"

    let program = ConfigurationManager.AppSettings.Get "7ZipLocation"

    let printSeqInfo (s:seq<string>) =
        printf "{ "
        s |> Seq.take 3 |> Seq.iter (printf "%s ")
        printfn "... } - Total size: %i" (Seq.length s)

    let load =
        try
            let file = argv.[0]
            let minLength = Int32.Parse argv.[1]
            let maxLength = Int32.Parse argv.[2]
            let attackType =
                match argv.[3] with
                | "b" | "d" -> argv.[3]
                | _ -> invalidArg "attackType" argv.[3]
            file, minLength, maxLength, attackType
        with
            ex ->
                printfn "%s" showUsage
                Environment.Exit 1
                "", 0, 0, ""

    let file, minLength, maxLength, attackType = load

    // Adds password to file
    let p1 = new Process()
    p1.StartInfo.FileName <- program
    p1.StartInfo.Arguments <- "a -p" + "abc" + " -y " + file + " BruteForceCracker.exe.config"
    p1.StartInfo.RedirectStandardOutput <- true
    p1.StartInfo.UseShellExecute <- false
    p1.Start() |> ignore
    p1.WaitForExit()
    printfn "%i %s" p1.ExitCode (p1.StandardOutput.ReadToEnd())

    let mutable cracked = false

    let timer = Stopwatch()

    if not cracked && attackType = "d" then
        printfn "Starting dictionary cracker..."
        timer.Restart()

        // Sequences
        let dictLocation = ConfigurationManager.AppSettings.Get "DictionaryLocation"
        let dictSeq = File.ReadAllLines dictLocation |> Seq.ofArray

        printSeqInfo dictSeq

        let cpuWorkLength = Seq.length dictSeq / Environment.ProcessorCount
        let cpuWorkSeq = Seq.chunkBySize cpuWorkLength dictSeq

        printfn "\nProcessor count: %i - Workload per CPU: %i" Environment.ProcessorCount cpuWorkLength

        // Cracks password
        let mutable cancel = false

        let mutable counter = 0

        let bruteCrackAsync s a = async {
            let p2 = new Process()
            p2.StartInfo.FileName <- program
            p2.StartInfo.RedirectStandardOutput <- true
            p2.StartInfo.RedirectStandardError <- true
            p2.StartInfo.UseShellExecute <- false

            Seq.tryFind (fun n ->
                counter <- counter + 1
                p2.StartInfo.Arguments <- "x -p" + n + " -y -oOutput " + file
                p2.Start() |> ignore
                p2.WaitForExit()
           
                if p2.ExitCode = 0 then
                    printfn "%s %s" (p2.StandardOutput.ReadToEnd()) a
                    timer.Stop()
                    printfn "\nTotal execution time: %f\nNumber of tries: %i\nFINISHED." timer.Elapsed.TotalSeconds counter
                    cancel <- true
                    cracked <- true
                    true
                else
                    cancel) s |> ignore
        }

        for i in 0..(Seq.length cpuWorkSeq - 1) do
            Async.Start (bruteCrackAsync (cpuWorkSeq |> Seq.skip i |> Seq.head) ("seq" + string i))
    
    if not cracked && attackType = "b" then
        printfn "Starting brute force cracker..."
        timer.Restart()

        // Sequences
        let numSeq = { 0..9 } |> Seq.map string
        let lowerABCSeq = { 'a'..'z' } |> Seq.map string
//        let upperABCSeq = { 'A'..'Z' } |> Seq.map string
        let specialSeq = " !\"#$%&'()*+,-./:;<=>?@[\]^_`{|}~".ToCharArray() |> Seq.map string

//        printSeqInfo numSeq
        printSeqInfo lowerABCSeq
//        printSeqInfo upperABCSeq
//        printSeqInfo specialSeq

        let allSeqs =
//            numSeq
//            upperABCSeq
            lowerABCSeq
//            |> Seq.append specialSeq

        // Custom Sequences
        let mutable minLengthSeq = allSeqs

        for i in 1..(minLength-1) do
            minLengthSeq <-
                minLengthSeq
                |> Seq.collect (fun x -> allSeqs |> Seq.map (fun y -> x + y))

        let mutable finalSeq = minLengthSeq

        for i in minLength..(maxLength-1) do
            finalSeq <-
                finalSeq
                |> Seq.collect (fun x -> allSeqs |> Seq.map (fun y -> x + y))
                |> Seq.append finalSeq
    
        printfn "\n{ %s ... %s } - Total combinations: %i" (Seq.head finalSeq) (Seq.last finalSeq) (Seq.length finalSeq)

        let cpuWorkLength = Seq.length finalSeq / Environment.ProcessorCount
        let cpuWorkSeq = Seq.chunkBySize cpuWorkLength finalSeq

        printfn "\nProcessor count: %i - Workload per CPU: %i" Environment.ProcessorCount cpuWorkLength

        // Cracks password
        let mutable cancel = false

        let mutable counter = 0

        let bruteCrackAsync s a = async {
            let p2 = new Process()
            p2.StartInfo.FileName <- program
            p2.StartInfo.RedirectStandardOutput <- true
            p2.StartInfo.RedirectStandardError <- true
            p2.StartInfo.UseShellExecute <- false

            Seq.tryFind (fun n ->
                counter <- counter + 1
                p2.StartInfo.Arguments <- "x -p" + n + " -y -oOutput " + file
                p2.Start() |> ignore
                p2.WaitForExit()
           
                if p2.ExitCode = 0 then
                    printfn "%s %s" (p2.StandardOutput.ReadToEnd()) a
                    timer.Stop()
                    printfn "\nTotal execution time: %f\nNumber of tries: %i\nFINISHED." timer.Elapsed.TotalSeconds counter
                    cancel <- true
                    cracked <- true
                    true
                else
                    cancel) s |> ignore
        }

        for i in 0..(Seq.length cpuWorkSeq - 1) do
            Async.Start (bruteCrackAsync (cpuWorkSeq |> Seq.skip i |> Seq.head) ("seq" + string i))
    
    System.Console.Read() |> ignore
    0
