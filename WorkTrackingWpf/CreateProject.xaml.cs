using Microsoft.EntityFrameworkCore;
using WorkTracking.DAL.Data;
using WorkTracking.Model.Model;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Globalization;

namespace WorkTrackingWpf
{
    public partial class CreateProject : Page
    {
        private readonly AppDbContext _context;

        public CreateProject(AppDbContext context)
        {
            InitializeComponent();
            _context = context;

            Loaded += (s, e) => LoadData();
        }

        private void LoadData()
        {
            LoadCompanies();
            LoadContractTypes();
            LoadContractCurrencies();
            LoadProjects();
        }

        private void LoadProjects()
        {
            var projects = _context.Projects
                .Include(p => p.Company)
                .Include(p => p.ContractType)
                .ToList();

            if (projects == null || !projects.Any())
            {
                MessageBox.Show("Projeler bulunamadı.");
                return;
            }
            ProjectsDataGrid.ItemsSource = projects;
        }
        private void LoadCompanies()
        {
            var companies = _context.Companies.ToList();
            CompanyComboBox.ItemsSource = companies;
            CompanyComboBox.DisplayMemberPath = "CompanyName";
            CompanyComboBox.SelectedValuePath = "CompanyId";
        }

        private void LoadContractTypes()
        {
            var contractTypes = _context.ContractTypes.ToList();
            ContractTypeComboBox.ItemsSource = contractTypes;
            ContractTypeComboBox.DisplayMemberPath = "ContractTypeName";
            ContractTypeComboBox.SelectedValuePath = "ContractTypeId";
        }

        private void LoadContractCurrencies()
        {
            ContractCurrencyComboBox.ItemsSource = new List<string> { "TL", "USD", "EUR" };
        }

        private void ClearFormFields()
        {
            JobNumberTextBox.Clear();
            JobNameTextBox.Clear();
            ContractAmountTextBox.Clear();
            UnitAmountTextBox.Clear();
            CompanyComboBox.SelectedIndex = -1;
            ContractTypeComboBox.SelectedIndex = -1;
            ContractCurrencyComboBox.SelectedIndex = -1;
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

            decimal contractAmount = decimal.TryParse(ContractAmountTextBox.Text, out var parsedContractAmount) ? parsedContractAmount : 0;
            decimal unitAmount = decimal.TryParse(UnitAmountTextBox.Text, out var parsedUnitAmount) ? parsedUnitAmount : 0;

            decimal totalAmount = contractAmount * unitAmount; 

            var newProject = new Project
            {
                ProjectNumber = JobNumberTextBox.Text,
                ProjectName = JobNameTextBox.Text,
                ProjectStartTime = ProjectStartDatePicker.SelectedDate.Value, 
                ProjectEndTime = projectEndDate.HasValue ? projectEndDate.Value : (DateTime?)null,
                CompanyId = (int)CompanyComboBox.SelectedValue,
                ContractTypeId = (int)ContractTypeComboBox.SelectedValue,
                ContractAmount = contractAmount,
                UnitAmount = unitAmount,
                ContractCurrency = (ContractCurrency)ContractCurrencyComboBox.SelectedIndex,
                TotalAmount = totalAmount 
            };

            _context.Projects.Add(newProject);
            _context.SaveChanges();
            LoadProjects();
            ClearFormFields(); 
            MessageBox.Show("Proje başarıyla kaydedildi.");
        }


        private void UpdateProject_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsDataGrid.SelectedItem is Project selectedProject)
            {
                Console.WriteLine($"Selected project ID: {selectedProject.ProjectId}");

                UpdateProjectWindow updateWindow = new UpdateProjectWindow(_context, selectedProject.ProjectId);
                updateWindow.Show(); 

                LoadProjects();
            }
            else
            {
                MessageBox.Show("Lütfen bir proje seçiniz.");
            }
        }


        private void ProjectsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectsDataGrid.SelectedItem != null)
            {
                var selectedProject = ProjectsDataGrid.SelectedItem as Project;
                if (selectedProject != null)
                {
                    Console.WriteLine($"Seçilen proje ID: {selectedProject.ProjectId}");
                }
            }
        }
    

        private void SaveJobButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectSelectionComboBox.SelectedValue == null)
            {
                MessageBox.Show("Lütfen bir proje seçiniz.");
                return;
            }

            if (string.IsNullOrWhiteSpace(TaskNameTextBox.Text))
            {
                MessageBox.Show($"JobNameTextBox Text: {TaskNameTextBox.Text ?? "null"}");
                MessageBox.Show("Lütfen bir iş adı giriniz.");
                return;
            }

            int selectedProjectId = (int)ProjectSelectionComboBox.SelectedValue;

            var newProjectTask = new ProjectTask
            {
                TaskName = TaskNameTextBox.Text.Trim(),
                ProjectId = selectedProjectId
            };

            _context.ProjectTasks.Add(newProjectTask);
            _context.SaveChanges();

            TaskNameTextBox.Clear();
        }

        private void ProjectSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectSelectionComboBox.SelectedValue == null)
                return;

            int selectedProjectId = (int)ProjectSelectionComboBox.SelectedValue;

            var projectTasks = _context.ProjectTasks
                .Where(task => task.ProjectId == selectedProjectId)
                .Select(task => task.TaskName)
                .ToList();
        }

        private string FormatCurrency(decimal value)
        {
            return value.ToString("N2", CultureInfo.InvariantCulture);
        }

        private void SaveContractTypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ContractTypeNameTextBox.Text))
            {
                MessageBox.Show("Lütfen bir sözleşme türü adı giriniz.");
                return;
            }

            var newContractType = new ContractType
            {
                ContractTypeName = ContractTypeNameTextBox.Text
            };

            _context.ContractTypes.Add(newContractType);
            _context.SaveChanges();
            MessageBox.Show("Sözleşme türü başarıyla kaydedildi.");

            ContractTypeNameTextBox.Clear();
        }

        // private void ToggleProjectStatus_Click(object sender, RoutedEventArgs e)
        // {
        //     if (ProjectsDataGrid.SelectedItem is Project selectedProject)
        //     {
        //         // Toggle project status
        //         selectedProject.IsActive = !selectedProject.IsActive;
        //
        //         // Save the changes
        //         _context.SaveChanges();
        //
        //         // Refresh the data grid to reflect changes
        //         ProjectsDataGrid.Items.Refresh();
        //
        //         MessageBox.Show("Project status has been toggled.");
        //     }
        //     else
        //     {
        //         MessageBox.Show("Please select a project.");
        //     }
        // }

    }
}
