using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WorkTracking.DAL.Data;
using WorkTracking.Model.Model;

namespace WorkTrackingWpf
{
    public partial class UpdateCompanyWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly Company _company;

        public UpdateCompanyWindow(AppDbContext context, Company company)
        {
            InitializeComponent();
            _context = context;
            _company = company;
            this.Topmost = true;

            CompanyNameTextBox.Text = _company.CompanyName;
            CompanyTitleTextBox.Text = _company.CompanyTitle;
            AddressTextBox.Text = _company.Address;
            TaxOfficeTextBox.Text = _company.TaxOffice;
            TaxNumberTextBox.Text = _company.TaxNumber;
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _company.CompanyName = CompanyNameTextBox.Text;
                _company.CompanyTitle = CompanyTitleTextBox.Text;
                _company.Address = AddressTextBox.Text;
                _company.TaxOffice = TaxOfficeTextBox.Text;
                _company.TaxNumber = TaxNumberTextBox.Text;

                await _context.SaveChangesAsync();
                MessageBox.Show("Firma başarıyla güncellendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Firma güncellenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
