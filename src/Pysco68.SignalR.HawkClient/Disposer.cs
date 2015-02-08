using System;
using System.Diagnostics;
using System.Threading;

namespace Pysco68.SignalR.HawkClient
{
    // Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
    //
    // Licensed under the Apache License, Version 2.0 (the "License"); you may not use this 
    // file except in compliance with the License. You may obtain a copy of the License at
    //
    //http://www.apache.org/licenses/LICENSE-2.0
    //
    // Unless required by applicable law or agreed to in writing, software distributed under 
    // the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF 
    // ANY KIND, either express or implied. See the License for the specific language governing 
    // permissions and limitations under the License.

    // NOTE: this is original work included from the SignalR project from Microsoft Open Technologies, Inc.
    // https://github.com/SignalR/SignalR
    //
    // In Project Microsoft.AspNet.SignalR.Core:
    // https://github.com/SignalR/SignalR/blob/master/src/Microsoft.AspNet.SignalR.Core/Infrastructure/Disposer.cs
    internal class Disposer : IDisposable
    {
        private static readonly object _disposedSentinel = new object();
        private object _disposable;
        public void Set(IDisposable disposable)
        {
            if (disposable == null)
            {
                throw new ArgumentNullException("disposable");
            }
            object originalFieldValue = Interlocked.CompareExchange(ref _disposable, disposable, null);
            if (originalFieldValue == null)
            {
                // this is the first call to Set() and Dispose() hasn't yet been called; do nothing
            }
            else if (originalFieldValue == _disposedSentinel)
            {
                // Dispose() has already been called, so we need to dispose of the object that was just added
                disposable.Dispose();
            }
            else
            {
#if !PORTABLE && !NETFX_CORE
                // Set has been called multiple times, fail
                Debug.Fail("Multiple calls to Disposer.Set(IDisposable) without calling Disposer.Dispose()");
#endif
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var disposable = Interlocked.Exchange(ref _disposable, _disposedSentinel) as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
    }    
}
