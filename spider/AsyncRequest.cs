using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace spider
{
    class RequestState
    {
        private const int BUFFER_SIZE = 131072;
        private byte[] _data = new byte[BUFFER_SIZE];
        private StringBuilder _stringBuilder = new StringBuilder();

        public HttpWebRequest Request { get; private set; }
        public string Url { get; private set; }
        public int Depth { get; private set; }
        public int Index { get; private set; }
        public Stream ResourceStream { get; set; }
        public StringBuilder Html
    }

    class AsyncRequest
    {
        private bool[] _reqsBusy = null;
        private uint _reqCount = 4;
        private bool _stop = false;
        private Object _locker;
        private Dictionary<string, int> _urlsLoaded;
        private Dictionary<string, int> _urlsUnload;
        private void DispatchWork()
        {
            if (_stop)
            {
                return;
            }
            for (uint i = 0; i < _reqCount; i++)
            {
                if (!_reqsBusy[i])
                {
                    RequestResource(i);
                }
            }
        }
        private void RequestResource(uint index)
        {
            int depth;
            string url = "";
            try
            {
                lock (_locker)
                {
                    if (_urlsUnload.Count <= 0)
                    {
                        _workingSignals.FinishWorking(index);
                        return;
                    }
                    _reqsBusy[index] = true;
                    _workingSignals.StartWorking(index);
                    depth = _urlsUnload.First().Value;
                    url = _urlsUnload.First().Key;
                    _urlsLoaded.Add(url, depth);
                    _urlsUnload.Remove(url);
                }
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = _method;
                req.Accept = _accept;
                req.UserAgent = _userAgent;
                RequestState rs = new RequestState(req, url, depth, index);
                var result = req.BeginGetResponse(new AsyncCallback(ReceivedResource), rs);
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, TimeOutCallback, rs, _maxTime, true);
            }
            catch (WebException we)
            {
                MessageBox.Show("RequestResource " + we.Message + url + we.Status);
            }
        }


    }
}
