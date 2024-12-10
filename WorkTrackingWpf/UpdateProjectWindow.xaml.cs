using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using WorkTracking.DAL.Data;
using WorkTracking.Model.Model;

namespace WorkTrackingWpf
{
    public partial class UpdateProjectWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly int _projectId;

        public UpdateProjectWindow(AppDbContext context, int projectId)
        {
            InitializeComponent();
            _context = context;
            _projectId = projectId;

            Loaded += (s, e) => LoadData();
        }

        private void LoadData()
        {
            var companies = _context.Companies.ToList();
            CompanyComboBox.Items.Clear(); 
            CompanyComboBox.ItemsSource = companies;
            CompanyComboBox.DisplayMemberPath = "CompanyName"; 
            CompanyComboBox.SelectedValuePath = "CompanyId";  

            var contractTypes = _context.ContractTypes.ToList();
            ContractTypeComboBox.Items.Clear(); 
            ContractTypeComboBox.ItemsSource = contractTypes;
            ContractTypeComboBox.DisplayMemberPath = "ContractTypeName"; 
            ContractTypeComboBox.SelectedValuePath = "ContractTypeId";  

            ContractCurrencyComboBox.Items.Clear();
            ContractCurrencyComboBox.ItemsSource = new List<string> { "TL", "USD", "EUR" };

            var project = _context.Projects
                .Include(p => p.Company)
                .Include(p => p.ContractType)
                .FirstOrDefault(p => p.ProjectId == _projectId);

            if (project != null)
            {
                JobNumberTextBox.Text = project.ProjectNumber;
                JobNameTextBox.Text = project.ProjectName;
                ProjectStartDatePicker.SelectedDate = project.ProjectStartTime;
                ProjectEndDatePicker.SelectedDate = project.ProjectEndTime;

                CompanyComboBox.SelectedValue = project.CompanyId;
                ContractTypeComboBox.SelectedValue = project.ContractTypeId;
                ContractCurrencyComboBox.SelectedItem = project.ContractCurrency.ToString(); 

                ContractAmountTextBox.Text = project.ContractAmount.ToString("N2");
                UnitAmountTextBox.Text = project.UnitAmount.ToString("N2");
            }
            else
            {
                MessageBox.Show("Proje bulunamadı.");
            }
        }

        private void SaveProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (CompanyComboBox.SelectedItem == null || ContractTypeComboBox.SelectedItem == null || ContractCurrencyComboBox.SelectedItem == null)
            {
                MessageBox.Show("Lütfen tüm zorunlu alanları doldurun.");
                return;
            }

            if (!ProjectStartDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Lütfen proje başlangıç tarihini seçiniz.");
                return;
            }

            DateTime? projectEndDate = ProjectEndDatePicker.SelectedDate;

            var existingProject = _context.Projects.FirstOrDefault(p => p.ProjectId == _projectId);

            if (existingProject != null)
            {
                existingProject.ProjectNumber = JobNumberTextBox.Text;
                existingProject.ProjectName = JobNameTextBox.Text;
                existingProject.ProjectStartTime = ProjectStartDatePicker.SelectedDate.Value;
                existingProject.ProjectEndTime = projectEndDate;
                existingProject.CompanyId = (int)CompanyComboBox.SelectedValue; 
                existingProject.ContractTypeId = (int)ContractTypeComboBox.SelectedValue;  
                existingProject.ContractAmount = decimal.TryParse(ContractAmountTextBox.Text, out var contractAmount) ? contractAmount : 0;
                existingProject.UnitAmount = decimal.TryParse(UnitAmountTextBox.Text, out var unitAmount) ? unitAmount : 0;
                existingProject.ContractCurrency = (ContractCurrency)Enum.Parse(typeof(ContractCurrency), ContractCurrencyComboBox.SelectedItem.ToString());

                decimal totalAmount = CalculateTotalAmount(existingProject.ContractAmount, existingProject.UnitAmount);
                existingProject.TotalAmount = totalAmount;

                _context.SaveChanges();
                MessageBox.Show("Proje başarıyla güncellendi.");
                Close();  
            }
            else
            {
                MessageBox.Show("Proje bulunamadı.");
            }
        }
        private decimal CalculateTotalAmount(decimal contractAmount, decimal unitAmount)
        {
            return contractAmount * unitAmount;
        }

    }
}
