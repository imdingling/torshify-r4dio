﻿using System.Linq;
using System.Windows.Controls;

using Torshify.Radio.Framework;

namespace Torshify.Radio.EchoNest.Views.Browse.Tabs
{
    public partial class AlbumViewSmall
    {
        #region Constructors

        public AlbumViewSmall()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            var viewModel = DataContext as AlbumViewModel;

            if (viewModel != null)
            {
                viewModel.UpdateCommandBar(dataGrid.SelectedItems.OfType<Track>());
            }
        }

        #endregion Methods
    }
}