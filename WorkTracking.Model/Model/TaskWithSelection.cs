using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkTracking.Model.Model
{
    public class TaskWithSelection
    {
        public int CheckId { get; set; }
        public NewTask Task { get; set; }
        public bool IsChecked { get; set; }
    }
}
