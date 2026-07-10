open System

let sleep (n:int) = System.Threading.Thread.Sleep(n)

let task_new (fn:unit -> unit) = System.Threading.Tasks.Task.Factory.StartNew(fn)


#time 
let t0 = task_new (fun _ -> sleep 3000)
let t1 = task_new (fun _ -> sleep 3000)
let t2 = task_new (fun _ -> sleep 3000)
let t3 = task_new (fun _ -> sleep 3000)
// t0.Start()
// t1.Start()
// t2.Start()
// t3.Start()

t0.Wait()
t1.Wait()
t2.Wait()
t3.Wait()
#time 

