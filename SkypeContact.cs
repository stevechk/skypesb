using SKYPE4COMLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SkypeAPI2
{
    //skype contact details
    public class SkypeContact
    {
        //create this object from a SKYPE4COM User object
        public SkypeContact(User skypeUser)
        {
            Handle = skypeUser.Handle;
            DisplayName = skypeUser.DisplayName;
            FullName = skypeUser.FullName;
            Mobile = skypeUser.PhoneMobile;
            switch (skypeUser.OnlineStatus)
            {
                case TOnlineStatus.olsOnline:
                    Image = "theme/online.png";
                    Online = true;
                    break;
                case TOnlineStatus.olsOffline:
                    Image = "theme/offline.png";
                    Online = false;
                    break;
                default:
                    Image = "theme/unknown.png";
                    Online = false;
                    break;
            }
        }

        public string Handle { get; set; }

        public string FullName { get; set; }
        public string DisplayName { get; set; }

        public string PreferredName { 
            get 
            { 
                string result = Handle;
                if (!String.IsNullOrEmpty(FullName))
                    result = FullName;
                if (!String.IsNullOrEmpty(DisplayName))
                    result = DisplayName;
                return result;
            }
        }

        public string Mobile { get; set; }

        public string Image { get; set; }

        public bool Online { get; set; }
    }

    //collection object, used for databinding to WPF grid
    public class SkypeContactList : ObservableCollection<SkypeContact> {}
}
