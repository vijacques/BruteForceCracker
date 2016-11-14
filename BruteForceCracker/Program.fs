open System
open System.Diagnostics
open System.IO
open System.Threading

[<EntryPoint>]
let main argv = 
    // Timer
    let timer = Stopwatch.StartNew()

    // Basic Information
    let program = "C:\Program Files\7-Zip\7z.exe"
    let password = "zz"
    let minLength = 2
    let maxLength = 4
    
    // Sequences
    let numSeq = { 0..9 } |> Seq.map string
    let lowerABCSeq = { 'a'..'z' } |> Seq.map string
    let upperABCSeq = { 'A'..'Z' } |> Seq.map string
    let specialSeq = " !\"#$%&'()*+,-./:;<=>?@[\]^_`{|}~".ToCharArray() |> Seq.map string

    let printSeqInfo (s:seq<string>) =
        printf "{ "
        s |> Seq.take 3 |> Seq.iter (printf "%s ")
        printfn "... } - Total size: %i" (Seq.length s)

    printSeqInfo numSeq
    printSeqInfo lowerABCSeq
    printSeqInfo upperABCSeq
    printSeqInfo specialSeq

    let allSeqs =
        numSeq
        //|> Seq.append upperABCSeq
        |> Seq.append lowerABCSeq

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


    // Adds password to file
    let p1 = new Process()
    p1.StartInfo.FileName <- program
    p1.StartInfo.Arguments <- "a -p" + password + " -y secure.7z BruteForceCracker.exe.config"
    p1.StartInfo.RedirectStandardOutput <- true
    p1.StartInfo.UseShellExecute <- false
    p1.Start() |> ignore
    p1.WaitForExit()
    printfn "%i %s" p1.ExitCode (p1.StandardOutput.ReadToEnd())

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
            p2.StartInfo.Arguments <- "x -p" + n + " -y -oOutput secure.7z"
            p2.Start() |> ignore
            p2.WaitForExit()
           
            if p2.ExitCode = 0 then
                printfn "%s %s" (p2.StandardOutput.ReadToEnd()) a
                timer.Stop()
                printfn "Total execution time: %f\nNumber of tries: %i" timer.Elapsed.TotalSeconds counter
                cancel <- true
                true
            else
                cancel) s |> ignore
    }

    printfn "Starting brute force cracking..."

    for i in 0..(Seq.length cpuWorkSeq - 1) do
        Async.Start (bruteCrackAsync (cpuWorkSeq |> Seq.skip i |> Seq.head) ("seq" + string i))
    
    System.Console.Read() |> ignore
    0
