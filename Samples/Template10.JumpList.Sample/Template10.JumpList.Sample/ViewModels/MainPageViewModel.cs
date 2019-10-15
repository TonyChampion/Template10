using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Template10.Navigation;

namespace Template10.JumpList.Sample.ViewModels
{
    public class MainPageViewModel : ViewModelBase, IMainPageViewModel
    {
        private string _title;
        public string Title {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            if(parameters.ContainsKey("key"))
            {
                Title = String.Format("Loaded JumpList Key - {0}", parameters["key"]);
            } else
            {
                Title = "Template 10 JumpList Demo";
            }
        }
    }
}
