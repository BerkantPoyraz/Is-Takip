using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkTracking.DAL.Data;
using WorkTracking.Model.Model;

namespace WorkTracking.DAL.Repositories
{
    public class ContractTypeRepository
    {
        private readonly AppDbContext _context;

        public ContractTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddContractTypeAsync(string contractTypeName)
        {
            var contractType = new ContractType
            {
                ContractTypeName = contractTypeName
            };

            _context.ContractTypes.Add(contractType);
            await _context.SaveChangesAsync();
        }
    }
}
