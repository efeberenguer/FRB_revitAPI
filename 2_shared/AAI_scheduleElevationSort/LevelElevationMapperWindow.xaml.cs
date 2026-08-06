using System.Windows;

namespace FRB
{
    public partial class LevelElevationMapperWindow : Window
    {
        // =========================================================
        // SELECTED OPTIONS
        // =========================================================

        public bool InternalDoorsSelected =>
            InternalDoorsCheckBox.IsChecked == true;

        public bool ExternalDoorsSelected =>
            ExternalDoorsCheckBox.IsChecked == true;

        public bool RoomsSelected =>
            RoomsCheckBox.IsChecked == true;

        public bool GrossInternalAreasSelected =>
            GrossInternalAreasCheckBox.IsChecked == true;

        public bool GrossExternalAreasSelected =>
            GrossExternalAreasCheckBox.IsChecked == true;

        public bool NetInternalAreasSelected =>
            NetInternalAreasCheckBox.IsChecked == true;

        public bool SpecialtyEquipmentSelected =>
            SpecialtyEquipmentCheckBox.IsChecked == true;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public LevelElevationMapperWindow()
        {
            InitializeComponent();
        }


        // =========================================================
        // SELECT ALL
        // =========================================================

        private void SelectAllButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            InternalDoorsCheckBox.IsChecked = true;
            ExternalDoorsCheckBox.IsChecked = true;

            RoomsCheckBox.IsChecked = true;

            GrossInternalAreasCheckBox.IsChecked = true;
            GrossExternalAreasCheckBox.IsChecked = true;
            NetInternalAreasCheckBox.IsChecked = true;

            SpecialtyEquipmentCheckBox.IsChecked = true;
        }


        // =========================================================
        // CLEAR ALL
        // =========================================================

        private void ClearAllButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            InternalDoorsCheckBox.IsChecked = false;
            ExternalDoorsCheckBox.IsChecked = false;

            RoomsCheckBox.IsChecked = false;

            GrossInternalAreasCheckBox.IsChecked = false;
            GrossExternalAreasCheckBox.IsChecked = false;
            NetInternalAreasCheckBox.IsChecked = false;

            SpecialtyEquipmentCheckBox.IsChecked = false;
        }


        // =========================================================
        // CANCEL
        // =========================================================

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }


        // =========================================================
        // RUN
        // =========================================================

        private void RunButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}