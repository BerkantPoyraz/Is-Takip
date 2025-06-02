using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkTracking.Model.Model
{
    public class ContractType
    {
        public int ContractTypeId { get; set; }
        public string ContractTypeName { get; set; }

        public List<Project> Projects { get; set; }
    }
}
