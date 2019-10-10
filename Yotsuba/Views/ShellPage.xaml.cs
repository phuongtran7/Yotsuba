using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

using WinUI = Microsoft.UI.Xaml.Controls;

using Yotsuba.Helpers;
using Yotsuba.Services;
using System.Collections.ObjectModel;
using Yotsuba.Core.Models;

namespace Yotsuba.Views
{
    public sealed partial class ShellPage : Page, INotifyPropertyChanged
    {
        private readonly KeyboardAccelerator _altLeftKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu);

        private BoardModel _selectedBoard;

        public BoardModel SelectedBoard
        {
            get { return _selectedBoard; }
            set
            {
                Set(ref _selectedBoard, value);
                OnPropertyChanged("SelectedBoard");
            }
        }

        ObservableCollection<BoardModel> BoardList { get; set; }

        public ShellPage()
        {
            InitializeComponent();
            DataContext = this;
            Initialize();

            // Test data
            BoardList.Add(new BoardModel(Guid.NewGuid().GetHashCode(), "Helo World"));
        }

        private void Initialize()
        {
            NavigationService.Frame = shellFrame;
            NavigationService.NavigationFailed += Frame_NavigationFailed;
            NavigationService.Navigated += Frame_Navigated;

            // Init BoardList
            BoardList = new ObservableCollection<BoardModel>();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Keyboard accelerators are added here to avoid showing 'Alt + left' tooltip on the page.
            // More info on tracking issue https://github.com/Microsoft/microsoft-ui-xaml/issues/8
            KeyboardAccelerators.Add(_altLeftKeyboardAccelerator);
            await Task.CompletedTask;
        }

        private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw e.Exception;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(SettingsPage))
            {
                // Set the Header directly if Settings is selected
                navigationView.Header = "Settings";
                return;
            }

            if (SelectedBoard == null)
            {
                // Disable Header
                navigationView.Header = null;
            }
        }

        private void OnItemInvoked(WinUI.NavigationView sender, WinUI.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                NavigationService.Navigate(typeof(SettingsPage));
                SelectedBoard = null;
                return;
            }

            // Get the selected Board in the BoardList and then send it over to the BoardPage for display
            SelectedBoard = BoardList.Where(b => b.BoardName == (string)args.InvokedItem).FirstOrDefault();
            NavigationService.Navigate(typeof(BoardPage), SelectedBoard.TaskList);
        }

        private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator() { Key = key };
            if (modifiers.HasValue)
            {
                keyboardAccelerator.Modifiers = modifiers.Value;
            }

            keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;
            return keyboardAccelerator;
        }

        private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var result = NavigationService.GoBack();
            args.Handled = result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void NewBoardButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            NewBoard_SplitView.IsPaneOpen = true;
        }

        private void NewBoardSaveButton_Clicked(object sender, RoutedEventArgs e)
        {
            var new_id = Guid.NewGuid().GetHashCode();
            var new_board = new BoardModel(new_id, NewBoardNameTextBox.Text);
            BoardList.Add(new_board);

            // Update database
            //DataAccess.AddBoard(new_id, NewBoardNameTextBox.Text);

            NewBoardNameTextBox.Text = string.Empty;
            NewBoard_SplitView.IsPaneOpen = false;
        }

        private void EditBoardButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            EditBoardNameTextBox.Text = SelectedBoard.BoardName;
            EditBoard_SplitView.IsPaneOpen = true;
        }

        private void EditBoardSaveButton_Clicked(object sender, RoutedEventArgs e)
        {
            SelectedBoard.BoardName = EditBoardNameTextBox.Text;
            //Re-invoke current page to trigger reload
            NavigationService.Navigate(typeof(BoardPage), SelectedBoard.TaskList);

            // Update Database
            //DataAccess.UpdateBoard(Current_Board.ID, Current_Board.BoardName);

            EditBoard_SplitView.IsPaneOpen = false;
        }

        private async void EditBoardDeleteButton_Clicked(object sender, RoutedEventArgs e)
        {
            ContentDialog deleteFileDialog = new ContentDialog
            {
                Title = "Delete board permanently?",
                Content = "If you delete this board, you won't be able to recover it. Do you want to delete it?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel"
            };

            ContentDialogResult result = await deleteFileDialog.ShowAsync();

            // Delete the file if the user clicked the primary button.
            // Otherwise, do nothing.
            if (result == ContentDialogResult.Primary)
            {
                // Update Database
                //DataAccess.DeleteBoard(SelectedBoard.ID);

                BoardList.Remove(SelectedBoard);
                SelectedBoard = null;

                // Navigate back to blank main page
                NavigationService.Navigate(typeof(MainPage));

                EditBoard_SplitView.IsPaneOpen = false;
            }
        }

        private void EditBoardCancelButton_Clicked(object sender, RoutedEventArgs e)
        {
            EditBoardNameTextBox.Text = string.Empty;
            EditBoard_SplitView.IsPaneOpen = false;
        }

        private void NewTaskButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            NewTask_SplitView.IsPaneOpen = true;
        }

        private void NewTaskSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var dates = WeekPicker.SelectedDates;
            var sorted = dates.OrderBy(x => x.Date); // Sort the selected dates
            string FormattedWeekString = $"{sorted.First().ToString("MM/dd/yyyy")} - {sorted.Last().ToString("MM/dd/yyyy")}";

            var new_task_id = Guid.NewGuid().GetHashCode();
            SelectedBoard.TaskList.Add(new TaskModel
            {
                ID = new_task_id,
                BoardID = SelectedBoard.ID,
                Title = NewTaskNameTextBox.Text,
                Description = NewTaskDescriptionTextBox.Text,
                Tag = NewTaskTagTextBox.Text,
                Category = FormattedWeekString,
            });

            // Update Database
            //DataAccess.AddTaskToBoard(new_task_id, SelectedBoard.ID, NewTaskNameTextBox.Text, NewTaskDescriptionTextBox.Text, NewTaskTagTextBox.Text, FormattedWeekString);

            // Clear the filled data
            NewTaskNameTextBox.Text = string.Empty;
            NewTaskDescriptionTextBox.Text = string.Empty;
            NewTaskTagTextBox.Text = string.Empty;
            WeekPicker.SelectedDates.Clear();

            // Close SplitView pane
            NewTask_SplitView.IsPaneOpen = false;

            NavigationService.Navigate(typeof(BoardPage), SelectedBoard.TaskList);
        }

        private void NewTaskCancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the filled data
            NewTaskNameTextBox.Text = string.Empty;
            NewTaskDescriptionTextBox.Text = string.Empty;
            NewTaskTagTextBox.Text = string.Empty;
            WeekPicker.SelectedDates.Clear();

            // Close SplitView pane
            NewTask_SplitView.IsPaneOpen = false;
        }

        private void WeekPicker_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            // Only allow two dates to be selected
            if (sender.SelectedDates.Count > 2)
            {
                // Remove the first selected
                sender.SelectedDates.RemoveAt(0);
            }
        }
    }
}
