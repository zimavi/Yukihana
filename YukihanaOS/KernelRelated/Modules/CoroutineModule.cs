// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System;
using System.Collections.Generic;
using Cosmos.System.Coroutines;

namespace YukihanaOS.KernelRelated.Modules
{
#if MOD_COROUTINES

    internal class CoroutineModule : IKernelModule
    {
        public string Name => nameof(CoroutineModule);

        public string Description => "Kernel module that manages coroutines";

        public bool Initialize(out string error)
        {
            error = null;
            return true;
        }

        public object SendMessage(params object[] args)
        {
            if (args[0] is uint method)
            {
                switch (method)
                {
                    case 0:             // initialize coroutine pool and add system thread
                        return initPool(args[1]);

                    case 1:             // start coroutine pool
                        startPool();
                        return true;

                    case 2:             // add coroutine to the pool
                        return addCoroutine(args[1]);

                    case 3:             // kill coroutine with id
                        return killCoroutine(args[1]);

                    default:
                        return false;
                }
            }

            return false;
        }

        public void Shutdown()
        {
            SendMessage(1);
        }

        #region Functions

        private bool initPool(object systemThread)
        {
            if(systemThread == null) 
                return false;
            if (systemThread is not Action thread)
                return false;

            CoroutinePool.Main.OnCoroutineCycle.Add(thread);
            return true;
        }

        private void startPool()
        {
            CoroutinePool.Main.StartPool();
        }

        private bool addCoroutine(object coroutine)
        {
            if (coroutine == null)
                return false;
            if (coroutine is not Coroutine executor)
                return false;

            CoroutinePool.Main.AddCoroutine(executor);
            return true;
        }

        private bool killCoroutine(object coroutine)
        {
            if (coroutine == null)
                return false;
            if (coroutine is not Coroutine executor)
                return false;

            CoroutinePool.Main.RemoveCoroutine(executor);
            return true;
        }
        #endregion
    }

#endif
}
