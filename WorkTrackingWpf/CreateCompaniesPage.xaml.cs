using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WorkTracking.DAL.Data;
using WorkTracking.Model.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace WorkTrackingWpf
{
    public partial class CreateCompaniesPage : Page
    {
        private readonly AppDbContext _dbContext;

        public CreateCompaniesPage()
        {
            InitializeComponent();
            _dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>();
            LoadCompanies();
        }

        private async void SaveCompanyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string companyName = CompanyNameTextBox.Text.Trim();
                string companyTitle = CompanyTitleTextBox.Text.Trim();
                string address = CompanyAddressTextBox.Text.Trim();
                string taxOffice = TaxOfficeTextBox.Text.Trim();
                string taxNumber = TaxNumberTextBox.Text.Trim();

                if (string.IsNullOrEmpty(companyName) || string.IsNullOrEmpty(companyTitle) || string.IsNullOrEmpty(address) || string.IsNullOrEmpty(taxOffice) || string.IsNullOrEmpty(taxNumber))
                {
                    MessageBox.Show("Lütfen tüm alanları doldurun.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var newCompany = new Company
                {
                    CompanyName = companyName,
                    CompanyTitle = companyTitle,
                    Address = address,
                    TaxOffice = taxOffice,
                    TaxNumber = taxNumber
                };

                _dbContext.Companies.Add(newCompany);
                await _dbContext.SaveChangesAsync();

                LoadCompanies();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCompanies()
        {
            try
            {
                var companies = await _dbContext.Companies.ToListAsync();
                CompanyDataGrid.ItemsSource = companies;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Firma bilgileri yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFields()
        {
            CompanyNameTextBox.Clear();
            CompanyTitleTextBox.Clear();
            CompanyAddressTextBox.Clear();
            TaxOfficeTextBox.Clear();
            TaxNumberTextBox.Clear();
        }

        private async void UpdateCompanyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CompanyDataGrid.SelectedItem is Company selectedCompany)
            {
                var updateWindow = new UpdateCompanyWindow(_dbContext, selectedCompany);
                if (updateWindow.ShowDialog() == true) // Eğer güncelleme başarılıysa
                {
                    await LoadCompanies(); // Şirketleri yeniden yükle
                }
            }
            else
            {
                MessageBox.Show("Lütfen bir firma seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

    }
}
