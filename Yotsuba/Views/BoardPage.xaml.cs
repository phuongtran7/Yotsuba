using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Yotsuba.Core.Models;
using Yotsuba.Core.Utilities;

namespace Yotsuba.Views
{
    public sealed partial class BoardPage : Page, INotifyPropertyChanged
    {
        public ObservableCollection<TaskModel> Items { get; set; }

        private TaskModel _selectedTask;

        public TaskModel SelectedTask
        {
            get { return _selectedTask; }
            set
            {
                Set(ref _selectedTask, value);
                OnPropertyChanged("SelectedTask");
            }
        }

        private HashSet<string> _availableTags;
        public HashSet<string> AvailableTags
        {
            get { return _availableTags; }
            set
            {
                _availableTags = value;
                OnPropertyChanged("AvailableTags");
            }
        }

        public string CurrentTag { get; set; }

        public BoardPage()
        {
            InitializeComponent();
            AvailableTags = new HashSet<string>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Items = (ObservableCollection<TaskModel>)e.Parameter;
            if (Items != null)
            {
                itemsCVS.Source = FormatData();
            }
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

        private ObservableCollection<GroupInfoList> FormatData()
        {
            var query = from item in this.Items
                        group item by item.Category into g
                        orderby g.Key
                        select new GroupInfoList(g) { Key = g.Key };

            ObservableCollection<GroupInfoList> groupList = new ObservableCollection<GroupInfoList>(query);
            foreach (var item in groupList)
            {
                item.Title = item.Key.ToString();
            }

            return groupList;
        }

        private void OnItemGridViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (sender.ContainerFromItem(sender.Items.LastOrDefault()) is GridViewItem container)
            {
                container.XYFocusDown = container;
            }
        }

        private void OnItemGridViewItemClick(object sender, ItemClickEventArgs e)
        {
            AvailableTags = new HashSet<string>(Items.Select(t => t.Tag).ToList());
            SelectedTask = (TaskModel)e.ClickedItem;
            EditTaskName_TextBox.Text = SelectedTask.Title;
            EditTaskDescription_TextBox.Text = SelectedTask.Description;
            foreach (var item in TagSelector.Items)
            {
                if (item.ToString() == SelectedTask.Tag)
                {
                    TagSelector.SelectedItem = item;
                    break;
                }
            }

            EditTaskSplitView.IsPaneOpen = true;
        }

        private void OnItemGridViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var gridView = (GridView)sender;

            if (gridView.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
            {
                if (GetIsNarrowLayoutState())
                {
                    wrapGrid.ItemWidth = gridView.ActualWidth - gridView.Padding.Left - gridView.Padding.Right;
                }
                else
                {
                    wrapGrid.ItemWidth = double.NaN;
                }
            }
        }

        private bool GetIsNarrowLayoutState()
        {
            return LayoutVisualStates.CurrentState == NarrowLayout;
        }

        private void EditTaskSaveButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (TagSelector.SelectedItem == null)
            {
                EmptyTip.Title = "Tag is empty.";
                EmptyTip.Subtitle = "Please choose or add new tag.";
                EmptyTip.IsOpen = true;
                return;
            }

            SelectedTask.Title = EditTaskName_TextBox.Text;
            SelectedTask.Description = EditTaskDescription_TextBox.Text;
            SelectedTask.Tag = TagSelector.SelectedItem.ToString();

            // If choose new week then replace it
            if (WeekPicker.SelectedDates.Count != 0)
            {
                if (WeekPicker.SelectedDates.Count < 2)
                {
                    EmptyTip.Title = "Week is not selected.";
                    EmptyTip.Subtitle = "Please make sure to choose start and end date.";
                    EmptyTip.IsOpen = true;
                    return;
                }
                var dates = WeekPicker.SelectedDates;
                var sorted = dates.OrderBy(x => x.Date); // Sort the selected dates
                string FormattedWeekString = $"{sorted.First().ToString("MM/dd/yyyy")} - {sorted.Last().ToString("MM/dd/yyyy")}";
                SelectedTask.Category = FormattedWeekString;
            }

            EditTaskName_TextBox.Text = string.Empty;
            EditTaskDescription_TextBox.Text = string.Empty;
            TagSelector.SelectedItem = null;
            WeekPicker.SelectedDates.Clear();

            itemsCVS.Source = FormatData();

            // Update Database
            DataAccess.UpdateTask(SelectedTask.ID, SelectedTask.BoardID, SelectedTask.Title, SelectedTask.Description, SelectedTask.Tag, SelectedTask.Category);

            // Close SplitView
            EditTaskSplitView.IsPaneOpen = false;
        }

        private void EditTaskCancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Discard everything
            EditTaskName_TextBox.Text = string.Empty;
            EditTaskDescription_TextBox.Text = string.Empty;
            TagSelector.SelectedItem = null;
            WeekPicker.SelectedDates.Clear();

            // Close SplitView
            EditTaskSplitView.IsPaneOpen = false;
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

        private async void EditTaskDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog deleteFileDialog = new ContentDialog
            {
                Title = "Delete task permanently?",
                Content = "If you delete this task, you won't be able to recover it. Do you want to delete it?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel"
            };

            ContentDialogResult result = await deleteFileDialog.ShowAsync();

            // Delete the file if the user clicked the primary button.
            // Otherwise, do nothing.
            if (result == ContentDialogResult.Primary)
            {
                // Update Database
                DataAccess.DeleteTaskFromBoard(SelectedTask.BoardID, SelectedTask.ID);

                Items.Remove(SelectedTask);
                itemsCVS.Source = FormatData();
                EditTaskSplitView.IsPaneOpen = false;
            }
        }

        private void TagSelector_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Text))
            {
                AvailableTags.Add(args.Text);
                CurrentTag = args.Text;
            }
            else
            {
                args.Handled = true;
            }
        }
    }

    public class GroupInfoList : List<object>
    {
        public GroupInfoList(IEnumerable<object> items) : base(items) { }
        public object Key { get; set; }
        public string Title { get; set; }
    }
}
