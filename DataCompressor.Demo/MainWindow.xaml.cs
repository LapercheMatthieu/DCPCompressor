using CommunityToolkit.Mvvm.DependencyInjection;
using DCPCompressor.AbstractDataInformations;
using DCPCompressor.AbstractDatas;
using DCPCompressor.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DCPCompressor.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Initializer();


            // Informations sur la prise en charge SIMD
            richTextBox.Text = $"Support SIMD:\n";
            richTextBox.Text += $"Vector<short>.Count = {Vector<short>.Count}\n";
            richTextBox.Text += $"Vector<int>.Count = {Vector<int>.Count}\n";
            richTextBox.Text += $"Vector<double>.Count = {Vector<double>.Count}\n\n";

            richTextBox.Text += "Appuyez sur l'un des boutons pour lancer les tests correspondants.";
        }

        private void RunStandardTest_Click(object sender, RoutedEventArgs e)
        {
            
            // Tests standard existants
            var tests = new SignalCompressionTests();
            var result = tests.RunCompressionTests();

                richTextBox.Text = result;

           /* var test = new BasicTests();
            test.Launch();*/
        }


    }
}