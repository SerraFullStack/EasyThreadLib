using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Libs
{
    /*
     Developed by Rafael Tonello at 2017-03
     E-mail: tonello.rafinha@gmail.com
    */

    public class EasyThread
    {
        public delegate void ActionWithArgs(object[] arg);
        public delegate object ActionWithReturnAndArg(object arg);
        public delegate object ActionWithReturn();

        /// <summary>
        /// List of instantiated (EasyThreads).
        /// </summary>
        public static List<EasyThread> threadList = new List<EasyThread>();
        /// <summary>
        /// Thread states:
        ///     noInit: Thread not started
        ///     running: Thread is running
        ///     exited: Thread has finish executing
        /// </summary>
        public enum ThreadStatus { noInit, running, exited }

        /// <summary>
        /// Description of the function performed by the thread
        /// </summary>
        /// <param name="sender">Current EasyThread (Sender)</param>
        /// <param name="parameters">Parameters to be sent to the function</param>
        public delegate void EasyThreadFun(EasyThread sender, object parameters);

        private bool _running = true;
        private ThreadStatus __status = ThreadStatus.noInit;
        private bool __pause = false;
        private EasyThreadFun __onEnd = null;

        private Thread thread;

        public Dictionary<string, object> Tags = new Dictionary<string, object>();


        /// <summary>
        /// Some funciton to be executed when thread has ended
        /// </summary>
        public EasyThreadFun onEnd
        {
            get { return __onEnd; }
            set { __onEnd = value; }
        }

        /// <summary>
        /// Default contructor. You will need to call "Start" method to start the ethread
        /// </summary>
        public EasyThread()
        {
            EasyThread.threadList.Add(this);
        }

        /// <summary>
        /// Contructor that auto starts the thread.
        /// </summary>
        /// <param name="fun">The thread function</param>
        /// <param name="runAsWhileTrue">Run the thread inside a while true.</param>
        /// <param name="thParams">Arguments that will passed in each "fun" call/param>
        public EasyThread(EasyThreadFun fun, bool runAsWhileTrue = true, object thParams = null, EasyThreadFun onEnd = null, ThreadPriority priority = ThreadPriority.Normal, int interval = -1)
        {
            EasyThread.threadList.Add(this);
            this.Start(fun, runAsWhileTrue, thParams, onEnd, priority, interval);
        }

        /// <summary>
        /// Starts the threads.
        /// </summary>
        /// <param name="fun">The thread function</param>
        /// <param name="runAsWhileTrue">Run the thread inside a while true.</param>
        /// <param name="thParams">Arguments that will passed in each "fun" call/param>
        public void Start(EasyThreadFun fun, bool runAsWhileTrue, object thParams = null, EasyThreadFun onEnd = null, ThreadPriority priority = ThreadPriority.Normal, int interval = -1)
        {
            this.onEnd = onEnd;

            if (this.canRun())
            {
                thread = new Thread(delegate ()
                {
                    this.__status = ThreadStatus.running;
                    if (runAsWhileTrue)
                    {
                        while (this.canRun())
                        {
                            if (!__pause)
                            {
                                fun(this, thParams);
                                if (interval > -1)
                                    Thread.Sleep(interval);
                            }
                            else
                            {
                                this.sleep(1);
                            }
                        }
                        this.__status = ThreadStatus.exited;
                    }
                    else
                    {
                        fun(this, thParams);
                        this.__status = ThreadStatus.exited;
                    }
                    if (this.onEnd != null)
                        this.onEnd(this, thParams);
                });
                thread.Priority = priority;
                thread.Start();
            }
        }

        /// <summary>
        /// Sleeps the thread by a interval
        /// </summary>
        /// <param name="sleepMs">Interval, in milisseconds</param>
        public void sleep(int sleepMs)
        {
            Thread.Sleep(sleepMs);
        }

        /// <summary>
        /// Return true when the threas can be runned. After the "stop" function call, this function will ever return false.
        /// </summary>
        /// <returns></returns>
        public bool canRun()
        {
            return _running;
        }

        /// <summary>
        /// Returns the thread status
        /// </summary>
        /// <returns></returns>
        public ThreadStatus getThreadStatus()
        {
            return this.__status;

        }

        public void pause()
        {
            __pause = true;

        }

        public void resume()
        {
            __pause = false;
        }

        public bool isPaused()
        {
            return __pause;
        }

        /// <summary>
        /// Stop the thread
        /// </summary>
        /// <param name="awaitStop">Indicates that the function needs to wait thread end.</param>
        public void stop(bool awaitStop = false)
        {
            _running = false;

            if (awaitStop)
            {
                while (this.getThreadStatus() == ThreadStatus.running)
                    Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Stop the thread
        /// </summary>
        /// <param name="timeout">The max time out to wait thread end. Uses 0 to not wait..</param>
        /// <returns>Returns true if the thread was ended inside the timeout or, in another case, false</returns>
        public bool stop(int timeout)
        {
            _running = false;

            DateTime startTime = DateTime.Now;



            if (timeout > 0)
            {
                while (this.getThreadStatus() == ThreadStatus.running)
                {
                    if (DateTime.Now.Subtract(startTime).TotalMilliseconds > timeout)
                        return false;

                    Thread.Sleep(1);
                }
            }

            return true;
        }

        /// <summary>
        /// Stop all threads
        /// </summary>
        /// <param name="await">This argument informs if the function needs to wait the end of all threads</param>
        public static void stopAllThreads(bool await)
        {
            for (int cont = 0; cont < EasyThread.threadList.Count; cont++)
                if (EasyThread.threadList[cont] != null)
                    EasyThread.threadList[cont].stop(await);
        }

        /// <summary>
        /// Cria um novo objeto EasyThread
        /// </summary>
        /// <param name="fun">Função a ser execuata</param>
        /// <param name="runAsWhileTrue">Indica se deve executar como um while true</param>
        /// <param name="thParams">Parametros que serão passados para a thread</param>
        /// <returns></returns>
        public static EasyThread StartNew(EasyThreadFun fun, bool runAsWhileTrue, object thParams = null, EasyThreadFun onEnd = null, ThreadPriority priority = ThreadPriority.Normal)
        {
            return new EasyThread(fun, runAsWhileTrue, thParams, onEnd, priority);
        }

        /// <summary>
        /// Create a Thread auto timerized, like a timer. The minimun time precision is 10 milisseconds. Can be used when a long interval ins needed.
        /// The timer can be stopped in any moment.
        /// </summary>
        /// <param name="fun">The function to be called</param>
        /// <param name="interval">The interval to execute function (the minimum value is 10 milisseconds)</param>
        /// <param name="thParams">Optional parameters to be passed in each "fun" call</param>
        /// <param name="onEnd">A optional function to be called when the timer is stopped</param>
        /// <param name="priority">The thread priority.</param>
        /// <returns></returns>
        public static EasyThread StartTimer(EasyThreadFun fun, int interval, object thParams = null, EasyThreadFun onEnd = null, ThreadPriority priority = ThreadPriority.Normal)
        {
            return StartNew(delegate (EasyThread snd, object args)
            {
                if (snd.Tags.ContainsKey("timeout"))
                {
                    Thread.Sleep(10);
                    if (DateTime.Now.Subtract((DateTime)snd.Tags["timeout"]).TotalMilliseconds >= interval)
                    {
                        snd.Tags["timeout"] = DateTime.Now;
                        fun(snd, args);
                    }
                }
                else
                {
                    snd.Tags["timeout"] = DateTime.Now;
                    fun(snd, args);
                }

            }, true, thParams, onEnd, priority);
        }


        public static void timeOut(Action action, int timeout)
        {
            EasyThread.StartNew(delegate (EasyThread sender, object args)
            {
                sender.sleep(timeout);
                action.Invoke();
            }, false);


        }


        
        public static void RunAsync(ActionWithReturnAndArg[] actions, ActionWithArgs onDone, object argsToActions = null)
        {
            object[] resps = new object[actions.Length];
            int respsRemain = actions.Length;

            StartNew(delegate (EasyThread sender, object args) {
                for (int c = 0; c < actions.Length; c++)
                {
                    StartNew(delegate (EasyThread sender2, object args2)
                    {
                        object ret = actions[(int)args2](argsToActions);
                        respsRemain--;
                    }, false, c);
                }

                while (respsRemain > 0)
                    sender.sleep(1);

                onDone(resps);
                
            }, false);

        }

        public static void RunAsync(ActionWithReturn[] actions, ActionWithArgs onDone)
        {
            object[] resps = new object[actions.Length];
            int respsRemain = actions.Length;

            StartNew(delegate (EasyThread sender, object args)
            {
                for (int c = 0; c < actions.Length; c++)
                {
                    StartNew(delegate (EasyThread sender2, object args2)
                    {
                        object ret = actions[(int)args2]();
                        respsRemain--;
                    }, false, c);
                }

                while (respsRemain > 0)
                    sender.sleep(1);

                onDone(resps);
            }, false);
        }

        public static void RunAsync(ActionWithReturnAndArg action, ActionWithArgs onDone, object argsToAction = null)
        {
            RunAsync(new ActionWithReturnAndArg[] { action }, onDone, argsToAction);
        }

        public static void RunAsync(ActionWithReturn action, ActionWithArgs onDone)
        {
            RunAsync(new ActionWithReturn[] { action }, onDone);
        }
    }
}
