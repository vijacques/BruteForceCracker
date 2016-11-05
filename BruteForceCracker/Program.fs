open System
open System.Diagnostics
open System.Threading

[<EntryPoint>]
let main argv = 
    // Timer
    let timer = Stopwatch.StartNew()

    // Basic Information
    let program = "C:\Program Files\7-Zip\7z.exe"
    let password = "Hf3"
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

    let allSeqs = numSeq |> Seq.append upperABCSeq |> Seq.append lowerABCSeq

    // Custom Sequences
    let mutable minLengthSeq = allSeqs

    for i in 1..(minLength-1) do
        minLengthSeq <-
            minLengthSeq
            |> Seq.map (fun x -> allSeqs |> Seq.map (fun y -> x + y))
            |> Seq.concat

    let mutable finalSeq = minLengthSeq

    for i in minLength..(maxLength-1) do
        finalSeq <-
            finalSeq
            |> Seq.map (fun x -> allSeqs |> Seq.map (fun y -> x + y))
            |> Seq.concat
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
    let cancToken = new CancellationTokenSource()

    let bruteCrackAsync s a = async {
        Seq.iter (fun n ->
            let p2 = new Process()
            p2.StartInfo.FileName <- program
    
            p2.StartInfo.Arguments <- "x -p" + n + " -y secure.7z"
//            p2.StartInfo.RedirectStandardOutput <- true
            p2.StartInfo.UseShellExecute <- false
            p2.Start() |> ignore
            p2.WaitForExit()
           
            if p2.ExitCode = 0 then
                printfn "%i %s %s" p2.ExitCode (p2.StandardOutput.ReadToEnd()) a
                timer.Stop()
                cancToken.Cancel()) s
//            else
//                printfn "wrong password: %s" n) s

//            let f = Seq.tryFind (fun n -> n = "Hf3") s
//        match f with
//        | Some s ->
//            printfn "found Hf3!!! - %s" a
//            timer.Stop()
//            cancToken.Cancel()
//        | None -> ()
    }

    Async.Start (bruteCrackAsync (cpuWorkSeq |> Seq.head) "seq1", cancToken.Token)
    Async.Start (bruteCrackAsync (cpuWorkSeq |> Seq.skip 1 |> Seq.head) "seq2", cancToken.Token)
    Async.Start (bruteCrackAsync (cpuWorkSeq |> Seq.skip 2 |> Seq.head) "seq3", cancToken.Token)
    Async.Start (bruteCrackAsync (cpuWorkSeq |> Seq.skip 3 |> Seq.head) "seq4", cancToken.Token)

    printfn "Total execution time: %f" timer.Elapsed.TotalSeconds
    System.Console.Read() |> ignore
    0
