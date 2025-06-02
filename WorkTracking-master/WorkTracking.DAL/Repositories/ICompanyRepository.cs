using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkTracking.Model.Model;

namespace WorkTracking.DAL.Repositories
{
    public interface ICompanyRepository
    {
        public class CompanyService
        {
            private readonly ICompanyRepository _companyRepository;

            public interface ICompanyRepository
            {
                Task<List<Company>> GetAllCompaniesAsync();
                Task<Company> GetCompanyByIdAsync(int companyId);
                Task AddCompanyAsync(Company company);
                Task UpdateCompanyAsync(Company company);
                Task DeleteCompanyAsync(int companyId);
            }

        }

    }
}
