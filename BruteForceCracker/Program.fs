open System.Diagnostics

[<EntryPoint>]
let main argv = 
    let program = "C:\Program Files\7-Zip\7z.exe"
    let password = "H"
    let passLength = 2

    let numSeq = { 0..9 } |> Seq.map (fun n -> string n)
    let lowerABCSeq = { 'a'..'z' } |> Seq.map (fun n -> string n)
    let upperABCSeq = { 'A'..'Z' } |> Seq.map (fun n -> string n)

    let p1 = new Process()
    p1.StartInfo.FileName <- program
    p1.StartInfo.Arguments <- "a -p" + password + " -y secure.7z BruteForceCracker.exe.config"
    p1.StartInfo.RedirectStandardOutput <- true
    p1.StartInfo.UseShellExecute <- false
    p1.Start() |> ignore
    p1.WaitForExit()
    printfn "%i %s" p1.ExitCode (p1.StandardOutput.ReadToEnd())
    
    let p2 = new Process()
    p2.StartInfo.FileName <- program
    
    let force seq currentLength =
        seq
        |> Seq.tryFind
            (fun n ->
                p2.StartInfo.Arguments <- "x -p" + string n + " -y secure.7z"
                p2.StartInfo.RedirectStandardOutput <- true
                p2.StartInfo.UseShellExecute <- false
                p2.Start() |> ignore
                p2.WaitForExit()
           
                if p2.ExitCode = 0 then
                    printfn "%i %s" p2.ExitCode (p2.StandardOutput.ReadToEnd())
                    true
                else
                    printfn "wrong password: %s" n
                    false)

    let rec combine seqs currentLength maxLength =
        match force (Seq.head seqs) currentLength with
        | Some n ->
            System.Console.ReadKey() |> ignore
            System.Environment.Exit 0
        | None ->
            if not (Seq.isEmpty (Seq.tail seqs)) then
                combine (Seq.tail seqs) (currentLength+1) maxLength
            
    combine [numSeq; lowerABCSeq; upperABCSeq] 1 2
    
    0
