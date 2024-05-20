using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STimg.Helpers;
using System.Windows.Input;
using STimg.View;
using prob.Properties;

namespace STimg.ViewModel
{
   public class NavigationVM : BaseVM
    {
        private object _currentView;
        public ICommand EditCommand { get; set; }
        public ICommand HelpCommand { get; set; }
        public ICommand SamplesCommand { get; set; }
        public NavigationVM()
        {
            EditCommand = new RelayCommand(Edit);
            HelpCommand = new RelayCommand(Help);
            SamplesCommand = new RelayCommand(Samples);
            CurrentView = new EditPageVM();
        }

        public object CurrentView
        {
            get { return _currentView; }
            set { _currentView = value; OnPropertyChanged(); }
        }

        private void Edit(object obj) => CurrentView = new EditPageVM();
        private void Help(object obj) => CurrentView = new HelpPageVM();
        private void Samples(object obj) => CurrentView = new SamplesPageVM();
    }
}
