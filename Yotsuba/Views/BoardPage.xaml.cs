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

namespace Yotsuba.Views
{
    public sealed partial class BoardPage : Page, INotifyPropertyChanged
    {
        public ObservableCollection<TaskModel> Items { get; set; }

        public BoardPage()
        {
            InitializeComponent();
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
            var item = (TaskModel)e.ClickedItem;

            //EditTaskName_TextBox.Text = item.Title;
            //EditTaskDescription_TextBox.Text = item.Description;
            //EditTaskTagTextBox.Text = item.Tag;
            //CurrentEditTaskID = item.ID;

            //EditTaskSplitView.IsPaneOpen = true;
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
    }

    public class GroupInfoList : List<object>
    {
        public GroupInfoList(IEnumerable<object> items) : base(items) { }
        public object Key { get; set; }
        public string Title { get; set; }
    }
}
