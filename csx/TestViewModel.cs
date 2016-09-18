using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace csx
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NotifyPropertyChangedAttribute : Attribute
    {
    }

    public class TestViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public double NoNotify { get; set; }

        private double _x;

        [NotifyPropertyChanged]
        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        [NotifyPropertyChanged]
        public double Y { get; set; }

        private double _z;

        [NotifyPropertyChanged]
        public double Z
        {
            get { return _z; }
        }

        
    }
}
