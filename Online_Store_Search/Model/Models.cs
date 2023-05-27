using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Store_Search.Model
{
    public interface IProduct
    {
        public string Mark { get; set; }
        public string Model { get; set; }
        public int Price { get; set; }
        dynamic DynamicProperties { get; }

    }
    public class Car : IProduct
    {
        public int Id { get; set; }
        public string Mark { get; set; }
        public string Model { get; set; }
        public int Price { get; set; }
        public string Engine { get; set; }
        public int HorsePower { get; set; }
        public string CoupeType { get; set; }
        public dynamic DynamicProperties => this;

    }

    public class Laptop : IProduct
    {
        public int Id { get; set; }
        public string Mark { get; set; }
        public string Model { get; set; }
        public string Processor { get; set; }
        public string RAM { get; set; }
        public string GPU { get; set; }
        public string MonitorResolution { get; set; }
        public int Price { get; set; }
        public dynamic DynamicProperties => this;

    }
    public class TV : IProduct
    {
        public int Id { get; set; }
        public string Mark { get; set; }
        public string Model { get; set; }
        public string Resolution { get; set; }
        public int Size { get; set; }
        public string DisplayType { get; set; }
        public int Price { get; set; }
        public dynamic DynamicProperties => this;

    }


}
