﻿using System.Windows.Controls;
using System.Windows.Input;

using Torshify.Radio.Framework.Controls;

namespace Torshify.Radio.EchoNest.Browse
{
    public partial class ArtistBrowseViewSmall : UserControl
    {
        #region Constructors

        public ArtistBrowseViewSmall()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _albumsListControl.Items.Count; i++)
            {
                var dgContainer = _albumsListControl.ItemContainerGenerator.ContainerFromIndex(i);

                if (dgContainer != null)
                {
                    var dg = dgContainer.FindVisualDescendantByType<DataGrid>();

                    if (dg != e.Source)
                    {
                        if (Keyboard.Modifiers != ModifierKeys.Control)
                        {
                            dg.SelectedItem = null;
                        }
                    }
                }
            }
        }

        #endregion Methods
    }
}