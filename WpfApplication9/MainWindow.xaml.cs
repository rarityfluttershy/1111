using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Win32;

namespace WpfApplication9
{
    public partial class MainWindow : Window, IDialogService
    {
        public MainWindowViewModel MainWindowViewModel
        {
            get { return (MainWindowViewModel) this.DataContext; }
        }

        public MainWindow()
        {
            SimpleIoc.Default.Register<IDialogService>(() => this);
            InitializeComponent();
        }

        private void ImagePicker_OnClick(object sender, MouseButtonEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                                "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                                "Portable Network Graphic (*.png)|*.png";
            if (fileDialog.ShowDialog() == true)
            {
                MainWindowViewModel.SelectedItem.ImageUri = fileDialog.FileName;
            }
        }

        public void ShowSuccessMessage(string text)
        {
            MessageBox.Show(text, "Success!");
        }
    }

    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<MainWindowViewModel>();
            SimpleIoc.Default.Register<IStorage, BinaryLocalStorage>();
        }

        public MainWindowViewModel MainWindowViewModel
        {
            get { return new MainWindowViewModel(); }
        }
    }

    public class LibraryItemViewModel : ViewModelBase
    {
        private string name;
        private string imageUri;

        public string Name
        {
            get { return name; }
            set { Set(() => Name, ref name, value); }
        }

        public string ImageUri
        {
            get { return imageUri; }
            set { Set(() => ImageUri, ref imageUri, value); }
        }

        public LibraryItemViewModel()
        {
            Name = "New Item";
        }
    }

    [Serializable]
    public class LibraryItemDto
    {
        public string Name { get; set; }
        public string ImageUri { get; set; }
    }

    public class MainWindowViewModel : ViewModelBase
    {
        private LibraryItemViewModel selectedItem;
        private IStorage storage;
        private IDialogService dialogService;

        public ICommand AddItemCommand
        {
            get { return new RelayCommand(() => LibraryItems.Add(new LibraryItemViewModel()));}
        }

        public ICommand DeleteCommand
        {
            get { return new RelayCommand(() => LibraryItems.Remove(SelectedItem)); }
        }

        public ICommand SaveCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    storage.Save(LibraryItems);
                    dialogService.ShowSuccessMessage("Your data has been successfully saved!");
                });
            }
        }

        public ObservableCollection<LibraryItemViewModel> LibraryItems { get; private set; }

        public LibraryItemViewModel SelectedItem
        {
            get { return selectedItem; }
            set { Set(() => SelectedItem, ref selectedItem, value); }
        }

        public MainWindowViewModel()
        {
            this.storage = SimpleIoc.Default.GetInstance<IStorage>();
            this.dialogService = SimpleIoc.Default.GetInstance<IDialogService>();
            LibraryItems = new ObservableCollection<LibraryItemViewModel>(storage.SavedItems);
            if (!LibraryItems.Any()) LibraryItems.Add(new LibraryItemViewModel());
            SelectedItem = LibraryItems.First();
        }
    }

    public interface IDialogService
    {
        void ShowSuccessMessage(string text);
    }

    public interface IStorage
    {
        IEnumerable<LibraryItemViewModel> SavedItems { get; }
        void Save(IEnumerable<LibraryItemViewModel> items);
    }

    public class BinaryLocalStorage : IStorage
    {
        private const string FileName = "LibraryItems.some-extension";

        public IEnumerable<LibraryItemViewModel> SavedItems
        {
            get
            {
                if (!File.Exists(FileName))
                {
                    return new LibraryItemViewModel[] {};
                }
                try
                {
                    var items = DeserializeFile();
                    return items.Select(it => new LibraryItemViewModel
                    {
                        Name = it.Name,
                        ImageUri = it.ImageUri
                    });
                }
                catch (SerializationException)
                {
                    
                }
                return new LibraryItemViewModel[] { };
            }
        }

        private IEnumerable<LibraryItemDto> DeserializeFile()
        {
            using (var stream = File.Open(FileName, FileMode.Open))
            {
                var formatter = new BinaryFormatter();
                var items = (List<LibraryItemDto>)formatter.Deserialize(stream);
                return items;
            }
        }

        public void Save(IEnumerable<LibraryItemViewModel> items)
        {
            using (var stream = File.Open(FileName, FileMode.Create))
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, items.Select(it => new LibraryItemDto
                {
                    Name = it.Name,
                    ImageUri = it.ImageUri
                }).ToList());
            }
        }
    }
}
