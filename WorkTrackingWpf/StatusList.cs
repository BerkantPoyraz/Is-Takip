using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkTracking.Model.Model;

namespace WorkTrackingWpf
{
    public static class StatusList
    {
        public static List<Status> AllStatuses => Enum.GetValues(typeof(Status)).Cast<Status>().ToList();
    }

}
