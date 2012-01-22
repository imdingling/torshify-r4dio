﻿using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Torshify.Radio.Core.Views.Stations
{
    [Export(typeof(StationsView))]
    public partial class StationsView : UserControl
    {
        public StationsView()
        {
            InitializeComponent();
        }

        [Import]
        public StationsViewModel Model
        {
            get { return DataContext as StationsViewModel; }
            set { DataContext = value; }
        }
    }
}
