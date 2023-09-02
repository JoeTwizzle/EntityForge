//namespace EntityForge.Jobs
//{
//    internal sealed class Boss
//    {
//        public readonly int MainThreadId;
//        public Boss()
//        {
//            MainThreadId = Environment.CurrentManagedThreadId;
//        }

//        public void Schedule()
//        {
//            if (MainThreadId != Environment.CurrentManagedThreadId)
//            {
//                throw new InvalidOperationException("Can only be called from Main Thread");
//            }
//        }

//        public struct ScheduledJob
//        {
//            public Action<bool> SchedulePredicate;
//            public Action Job;
//        }

//        public void Dispatch()
//        {

//        }

//    }
//}
////    class MyClass
////    {
////        void a()
////        {
////            SetMaxThreads(Environment.ProcessorCount);
////            Schedule(UpdatePlayer, UpdateEnemies);
////            Schedule(UpdatePhysics);
////            Schedule(UpdateUi, UpdateNetwork);
////            Schedule(RenderGame);
////            Dispatch();
////            //UpdatePlayer, UpdateEnemies
////            //UpdatePlayer, UpdateEnemies, UpdateAim, UpdatePathfinding
////            //UpdatePlayer, UpdateEnemies
////            //UpdatePhysics
////            //UpdateUi, UpdateNetwork
////            //RenderGame
////        }

////        void SetMaxThreads(int c)
////        {

////        }

////        void Schedule(params Action[] actions)
////        {

////        }

////        void DispatchCallback(Action callback, params Action[] actions)
////        {

////        }

////        void Dispatch()
////        {

////        }

////        void UpdateEnemies()
////        {
////            DispatchCallback(() => { }, UpdateAim, UpdatePathfinding);
////        }

////        void UpdateAim()
////        {

////        }

////        void UpdatePlayer()
////        {

////        }

////        void UpdatePathfinding()
////        {

////        }

////        void UpdateUi()
////        {

////        }

////        void UpdatePhysics()
////        {

////        }

////        void UpdateNetwork()
////        {

////        }

////        void RenderGame()
////        {

////        }

////    }
////}
