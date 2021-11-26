using System;
using System.Threading;
using System.Threading.Tasks;

namespace SelfDriven.Queueable.TaskExecutor
{
    class Program
    {
        public class MyTaskRequest : ITaskRequest
        {
            public MyTaskRequest(string id, int delay)
            {
                Id = id;
                Delay = delay;
            }
            public string Id { get; set; }
            public int Delay { get; set; }
        }

        public class MyTaskExecutor : IQueueableTaskExecutor<MyTaskRequest>
        {
            public void Execute(MyTaskRequest request)
            {
                Log("Started Id '{0}', Delay{1}", request.Id, request.Delay);
                Thread.Sleep(request.Delay);
                Log("Finished Id '{0}'", request.Id);
            }

            private void Log(string v, params object[] args)
            {
                Console.WriteLine("[{0}]\t[{1}]\t[{2}]", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), Task.CurrentId, string.Format(v, args));
            }

            public void Initialize()
            {
                Log("Initialized Executor{0}", nameof(MyTaskExecutor));
            }
        }

        static void Main(string[] args)
        {
            var qte = TaskQueue<MyTaskExecutor, MyTaskRequest>.CreateInstance();
            //First task request will kickstart the execution.
            qte.Enqueue(new MyTaskRequest("1", 100));
            qte.Enqueue(new MyTaskRequest("2", 200));
            qte.Enqueue(new MyTaskRequest("3", 300));
            Thread.Sleep(620);
            //Tasks from this request will be executed by new task, as kickstarted by this request.
            qte.Enqueue(new MyTaskRequest("4", 500));
            qte.Enqueue(new MyTaskRequest("5", 400));
            qte.Enqueue(new MyTaskRequest("6", 400));
            qte.Enqueue(new MyTaskRequest("7", 400));
            //Task 5&6 will be paused as per the stop request.
            Thread.Sleep(620);
            qte.Stop();
            //Task 5&6 will be executed by new task, as kickstarted by this Start request.
            qte.Start();
            Console.ReadKey();
        }
    }
}
