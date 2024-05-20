using STimg.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using STimg.Helpers;
namespace STimg.ViewModel
{
    public class HelpPageVM : BaseVM
    {
        public ICommand OpenInstagramCommand { get; }
        public ICommand OpenMailCommand { get; }
        public ICommand OpenGitHubCommand { get; }

        public HelpPage HelpPage
        {
            get => default;
            set
            {
            }
        }

        public HelpPageVM()
        {
            OpenInstagramCommand = new RelayCommand(OpenInstagram);
            OpenMailCommand = new RelayCommand(OpenMail);
            OpenGitHubCommand = new RelayCommand(OpenGitHub);
        }

        private void OpenInstagram(object obj)
        {
            System.Diagnostics.Process.Start("https://www.instagram.com/sskaliy54/");
        }

        private void OpenMail(object obj)
        {
            System.Diagnostics.Process.Start("https://mail.google.com/mail/?view=cm&fs=1&to=tetiana.skalii.ki.2020@lpnu.ua");
        }

        private void OpenGitHub(object obj)
        {
            System.Diagnostics.Process.Start("https://github.com/sskaliy54/image-filtering");
        }
     }

}
