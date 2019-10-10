using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Yotsuba.Core.Models
{
    public class BoardModel: INotifyPropertyChanged
    {
        public int ID { get; set; }
        private string _boardname;
        public string BoardName
        {
            get { return _boardname; }
            set
            {
                _boardname = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TaskModel> TaskList { get; set; }
        public List<Tuple<string, HourModel>> Hours { get; set; }

        public BoardModel(int id, string name)
        {
            this.ID = id;
            this.BoardName = name;
            this.TaskList = new ObservableCollection<TaskModel>();
            this.Hours = new List<Tuple<string, HourModel>>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
