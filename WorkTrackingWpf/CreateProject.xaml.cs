using Microsoft.EntityFrameworkCore;
using WorkTracking.DAL.Data;
using WorkTracking.Model.Model;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using Microsoft.Extensions.Options;

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
            LoadTasks();
        }

        private void LoadProjects()
        {
            var projects = _context.Projects
                .Include(p => p.Company)
                .Include(p => p.ContractType)
                .ToList();

            ProjectsDataGrid.ItemsSource = projects;

            TaskSelectionComboBox.ItemsSource = projects;
            TaskSelectionComboBox.DisplayMemberPath = "ProjectName";
            TaskSelectionComboBox.SelectedValuePath = "ProjectId";
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

        private void LoadTasks()
        {
            var tasks = _context.Tasks.ToList();
            TaskSelectionListBox.ItemsSource = tasks.Select(t => new TaskWithSelection { Task = t }).ToList();
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

            decimal contractAmount = 0;
            decimal unitAmount = 0;
            decimal totalAmount = 0;

            if (decimal.TryParse(ContractAmountTextBox.Text, out var parsedContractAmount))
            {
                contractAmount = parsedContractAmount;
            }

            if (decimal.TryParse(UnitAmountTextBox.Text, out var parsedUnitAmount))
            {
                unitAmount = parsedUnitAmount;
            }

            totalAmount = contractAmount * unitAmount;

            ContractCurrency selectedCurrency;
            if (!Enum.TryParse(ContractCurrencyComboBox.SelectedItem.ToString(), out selectedCurrency))
            {
                MessageBox.Show("Geçersiz sözleşme para birimi seçildi.");
                return;
            }

            var newProject = new Project
            {
                ProjectNumber = JobNumberTextBox.Text,
                ProjectName = JobNameTextBox.Text,
                ProjectStartTime = ProjectStartDatePicker.SelectedDate.Value,
                ProjectEndTime = projectEndDate,
                CompanyId = (int)CompanyComboBox.SelectedValue,
                ContractTypeId = (int)ContractTypeComboBox.SelectedValue,
                ContractAmount = contractAmount,
                UnitAmount = unitAmount,
                ContractCurrency = selectedCurrency,
                TotalAmount = totalAmount,
                ProjectStatus = ProjectStatus.Aktif
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
                UpdateProjectWindow updateWindow = new UpdateProjectWindow(_context, selectedProject.ProjectId);
                updateWindow.Show();
                LoadProjects();
            }
            else
            {
                MessageBox.Show("Lütfen bir proje seçiniz.");
            }
        }

        private void SaveJobButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskSelectionComboBox.SelectedValue == null)
            {
                MessageBox.Show("Lütfen bir proje seçiniz.");
                return;
            }

            int selectedProjectId = (int)TaskSelectionComboBox.SelectedValue;

            var selectedTasks = TaskSelectionListBox.Items
                .OfType<TaskWithSelection>()
                .Where(t => t.IsChecked)
                .Select(t => t.Task)
                .ToList();

            if (!selectedTasks.Any())
            {
                MessageBox.Show("Lütfen en az bir görev seçiniz.");
                return;
            }

            foreach (var task in selectedTasks)
            {
                if (!_context.ProjectTasks.Any(pt => pt.ProjectId == selectedProjectId && pt.TaskId == task.TaskId))
                {
                    var newProjectTask = new ProjectTask
                    {
                        ProjectId = selectedProjectId,
                        TaskId = task.TaskId
                    };

                    _context.ProjectTasks.Add(newProjectTask);
                }
            }

            _context.SaveChanges();
            MessageBox.Show("Görevler projeye başarıyla atanmıştır.");
        }


        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            string taskName = SaveNewTaskBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(taskName))
            {
                MessageBox.Show("Görev adı boş olamaz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_context.Tasks.Any(t => t.TaskName == taskName))
            {
                MessageBox.Show("Bu adla bir görev zaten mevcut.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newTask = new NewTask { TaskName = taskName };
            _context.Tasks.Add(newTask);
            _context.SaveChanges();

            LoadTasks();
            MessageBox.Show("Görev başarıyla eklendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            SaveNewTaskBox.Clear();
        }

        private void ProjectSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TaskSelectionComboBox.SelectedValue == null)
                return;

            int selectedProjectId = (int)TaskSelectionComboBox.SelectedValue;

            var projectTasks = _context.ProjectTasks
                .Where(pt => pt.ProjectId == selectedProjectId)
                .Select(pt => pt.TaskId)
                .ToList();

            var tasksWithSelection = _context.Tasks
                .ToList()
                .Select(task => new TaskWithSelection
                {
                    Task = task,
                    IsChecked = projectTasks.Contains(task.TaskId)
                })
                .ToList();

            TaskSelectionListBox.ItemsSource = tasksWithSelection;
        }
        private async void SaveContractTypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ContractTypeNameTextBox.Text))
            {
                MessageBox.Show("Lütfen bir sözleşme türü adı giriniz.");
                return;
            }

            var contractTypeName = ContractTypeNameTextBox.Text;

            var newContractType = new ContractType
            {
                ContractTypeName = contractTypeName
            };

            _context.ContractTypes.Add(newContractType);
            await _context.SaveChangesAsync();

            MessageBox.Show("Sözleşme türü başarıyla kaydedildi!");
            ContractTypeNameTextBox.Clear();
        }

        private void SetStatusActive_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsDataGrid.SelectedItem is Project selectedProject)
            {
                selectedProject.ProjectStatus = ProjectStatus.Aktif;
                SaveProjectStatus(selectedProject); 
                LoadProjects();
            }
            else
            {
                MessageBox.Show("Lütfen bir proje seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SetStatusPassive_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsDataGrid.SelectedItem is Project selectedProject)
            {
                selectedProject.ProjectStatus = ProjectStatus.Pasif;
                SaveProjectStatus(selectedProject); 
                LoadProjects();
            }
            else
            {
                MessageBox.Show("Lütfen bir proje seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveProjectStatus(Project project)
        {
            var dbProject = _context.Projects.Find(project.ProjectId);
            if (dbProject != null)
            {
                dbProject.ProjectStatus = project.ProjectStatus; 
                _context.SaveChanges(); 
            }
            else
            {
                MessageBox.Show("Seçilen proje veritabanında bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
