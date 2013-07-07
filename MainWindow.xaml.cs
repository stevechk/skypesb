using SKYPE4COMLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SkypeAPI2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// 
    /// </summary>
    public partial class MainWindow : Window
    {
        private Skype skype = new Skype();
        private SkypeContactList contactList;

        private bool onlineOnly = true;
        private Call currentCall;

        public MainWindow()
        {
            //load theme from file
            LoadTheme();

            //standard WPF initialisation
            InitializeComponent();

            //any skype-specific startup logic (e.g. setting it to silent mode)
            SetupSkype();

            //create/update remaining UI components
            InitialiseUI();
        }

        private void Window_Close(object sender, CancelEventArgs e)
        {
            //disable silent mode again
            skype.SilentMode = false;
        }

        /// <summary>
        /// Load theme from disk. This allows us to skin the app at runtime
        /// </summary>
        private void LoadTheme()
        {
            using (FileStream fs = new FileStream("theme/theme.xaml", FileMode.Open)) 
            { 
                ResourceDictionary dic = (ResourceDictionary)XamlReader.Load(fs); 
                Resources.MergedDictionaries.Clear(); 
                Resources.MergedDictionaries.Add(dic); 
            }  
        }

        /// <summary>
        /// Skype setup code.
        /// </summary>
        private void SetupSkype()
        {
            //set silent mode - this prevent skype popping up
            skype.SilentMode = true;

            //subscribe to any incoming messages, so that we can log them
            skype.MessageStatus += skype_MessageStatus;
            skype.Attach();
        }

        /// <summary>
        /// Called when Skype receives an incoming message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="Status"></param>
        void skype_MessageStatus(ChatMessage message, TChatMessageStatus Status)
        {
            AddMessage(message);
        }

        /// <summary>
        /// UI initialisation
        /// </summary>
        private void InitialiseUI()
        {
            //attach handle to XAML contact list
            contactList = (SkypeContactList) this.Resources["contactList"];

            //query skype to populate current contact list & message history
            PopulateContactList();
            PopulateMessages();
        }

        /// <summary>
        /// Query Skype to get current users contact list
        /// </summary>
        private void PopulateContactList()
        {
            //clear any existing items
            contactList.Clear();

            //populate contact list
            foreach(User friend in skype.Friends)
                contactList.Add(new SkypeContact(friend));
        }

        /// <summary>
        /// Query skype for any message history
        /// </summary>
        private void PopulateMessages()
        {
            messages.Items.Clear();
            foreach (ChatMessage msg in skype.MissedMessages)
                AddMessage(msg);
        }

        /// <summary>
        /// Add a message to the message list
        /// </summary>
        /// <param name="message"></param>
        private void AddMessage(ChatMessage message)
        {
            string sent = message.Timestamp.ToShortTimeString();
            string msgText = "[" + sent + "] " + message.FromHandle + ":" + message.Body;

            //TODO - sort message list, so that latest message is always at the top
            messages.Items.Add(msgText);
        }


        #region Contact List Actions

        /// <summary>
        /// When the user clicks on a contact in the list, open up the call menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ContactMenuPopup();
        }

        /// <summary>
        /// Clicking on the icon in the header toggles the contact filter between all and online-only
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void columnHeader_Click(object sender, RoutedEventArgs e)
        {
            DataGridColumnHeader columnHeader = sender as DataGridColumnHeader;
            if (columnHeader != null)
            {
                //toggle online only filter
                if (columnHeader.DisplayIndex == 0)
                {
                    onlineOnly = !onlineOnly;
                    RefreshContactList();
                }
            }
        }

        /// <summary>
        /// Filter handling for the contact list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            SkypeContact contact = e.Item as SkypeContact;
            if (onlineOnly && contact.Online)
                e.Accepted = true;
            else if (!onlineOnly)
                e.Accepted = true;
            else
                e.Accepted = false;
        }

        /// <summary>
        /// Refresh view
        /// </summary>
        private void RefreshContactList()
        {
            //triggers the contact list to update
            CollectionViewSource.GetDefaultView(dgContactList.ItemsSource).Refresh();
        }

        #endregion

        #region Contact Popup menu

        /// <summary>
        /// Menu popup, when the user clicks a contact
        /// </summary>
        private void ContactMenuPopup()
        {
            //show menus
            contactMenu.Visibility = Visibility.Visible;
            
            //hide datagrid
            dgContactList.Focusable = false;
            dgContactList.IsHitTestVisible = false;
            dgContactList.Opacity = 0.5;
        }

        /// <summary>
        /// Hide popup menu, when the user clicks 'cancel'
        /// </summary>
        private void ContactMenuHide()
        {
            //hide menus
            contactMenu.Visibility = Visibility.Hidden;

            //show datagrid
            dgContactList.Focusable = true;
            dgContactList.IsHitTestVisible = true;
            dgContactList.Opacity = 1;
        }


        /// <summary>
        /// Click the call button - places a call through Skype
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Call_Click(object sender, RoutedEventArgs e)
        {
            SkypeContact contact = (SkypeContact)dgContactList.SelectedItem;

            try
            {
                //if no current call, then place new call
                if (currentCall == null)
                {
                    currentCall = skype.PlaceCall(contact.Handle);
                    callMenu.Header = "Hang Up";
                }
                else
                {
                    //if existing call, hang up
                    currentCall.Finish();
                    currentCall = null;
                    callMenu.Header = "Call";
                    ContactMenuHide();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                ContactMenuHide();
            }

        }

        /// <summary>
        /// Click to IM - TODO (depending on keyboard inteface)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Chat_Click(object sender, RoutedEventArgs e)
        {
            ContactMenuHide();
        }

        /// <summary>
        /// Click to cancel the popup menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ContactMenuHide();
        }

        #endregion

        #region Search Page

        /// <summary>
        /// When one of the items from the search result is selected, pop up the search menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchResult_Click(object sender, MouseButtonEventArgs e)
        {
            SearchMenuPopup();
        }

        /// <summary>
        /// Menu popup, when the user clicks a contact
        /// </summary>
        private void SearchMenuPopup()
        {
            //show menus
            searchMenu.Visibility = Visibility.Visible;

            //hide datagrid
            searchResult.Focusable = false;
            searchResult.IsHitTestVisible = false;
            searchResult.Opacity = 0.5;
        }

        /// <summary>
        /// Hide popup menu, when the user clicks 'cancel'
        /// </summary>
        private void SearchMenuHide()
        {
            //hide menus
            searchMenu.Visibility = Visibility.Hidden;

            //show datagrid
            searchResult.Focusable = true;
            searchResult.IsHitTestVisible = true;
            searchResult.Opacity = 1;
        }

        /// <summary>
        /// Search for Skype users
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            //clear any old result
            searchResult.Items.Clear();

            //add all the new search results
            IUserCollection userResult = skype.SearchForUsers(searchText.Text);
            foreach (User user in userResult)
            {
                searchResult.Items.Add(user.Handle);
            }
        }

        /// <summary>
        /// Add the user from the search results to your Skype contact list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO - check this is the right way to do it - seems the API has changed recently
                //add the selected user to your skype contacts
                string userHandle = searchResult.SelectedItem.ToString();
                User user = skype.get_User(searchText.Text);
                user.BuddyStatus = TBuddyStatus.budFriend;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding contact:" + ex.Message);
            }

            //hide the popup menu again
            SearchMenuHide();
        }

        /// <summary>
        /// Close the search menu on Cancel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchMenuHide();
        }

        #endregion

    }
}
