using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApplication1
{
    public class WindowLog
    {
        private static WindowLog _logger;
        private static readonly object CreationLock = new object();

        public static WindowLog Default
        {
            get
            {

                if (_logger == null)
                {
                    lock (CreationLock)
                    {
                        if (_logger == null)
                            _logger = new WindowLog();
                    }
                }
                return _logger;
            }
        }

        private Action<string> _logComingAction;
        private Action<string,Point> _addLabelAction;
        private Action _reverseAction;

        public void WhenLogCommon(Action<string> logComingAction)
        {
            _logComingAction = logComingAction;
        }

        public void WhenAddLabel(Action<string,Point> addLabelAction)
        {
            _addLabelAction = addLabelAction;
        }

        public void Log(string log)
        {
            _logComingAction?.Invoke(log);
            Debug.WriteLine(log);
        }

        public void Log(string format, params object[] arg)
        {
            var log = string.Format(format, arg);
            Log(log);
        }

        public void AddLabel(string content, Point point)
        {
            _addLabelAction?.Invoke(content,point);
        }

        public void WhenReversePolygon(Action reverseActionCommand)
        {
            _reverseAction = reverseActionCommand;
        }

        public void ReversePolygon()
        {
            _reverseAction?.Invoke();
        }
    }

    public class Counter
    {
        private static Counter _counter;
        private static readonly object CreationLock = new object();

        public static Counter Default
        {
            get
            {

                if (_counter == null)
                {
                    lock (CreationLock)
                    {
                        if (_counter == null)
                            _counter = new Counter();
                    }
                }
                return _counter;
            }
        }

        public int Val { get; set; } = 1;

    }
}
