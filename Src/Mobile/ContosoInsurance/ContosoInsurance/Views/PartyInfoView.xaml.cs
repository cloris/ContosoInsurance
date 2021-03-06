﻿using System;
using System.Diagnostics;
using Xamarin.Forms;
using ContosoInsurance.Converters;
using ContosoInsurance.ViewModels;
using ContosoInsurance.Helpers;

namespace ContosoInsurance.Views
{
	public partial class PartyInfoView : ContentPage
    {
        private ClaimViewModel claimViewModel;
        private ClaimImageTypeModel type;
        private string fileName;

        public PartyInfoView (ClaimViewModel cv, ClaimImageTypeModel cmType)
		{
            Title = "Contoso Insurance";
            InitializeComponent ();

            claimViewModel = cv;
            type = cmType;
            BindingContext = claimViewModel;
            InitBottomToolBar();

            imageSource.SetBinding(Image.SourceProperty, new Binding { Source = BindingContext, Path = "Images", Converter = new PartyImageSourceConvert(), Mode = BindingMode.OneWay, ConverterParameter = type });
            cameraLabel.SetBinding(Label.IsVisibleProperty, new Binding { Source = BindingContext, Path = "Images", Converter = new PartyCameraHasImageConvert(), Mode = BindingMode.OneWay, ConverterParameter = type });

            string [] info = claimViewModel.getPartyImageTitleAndFilenameInfo(type);
            this.titleLabel.Text = info[0];
            fileName = info[1];
        }

        public void InitBottomToolBar()
        {
            var nextTapGestureRecognizer = new TapGestureRecognizer();
            nextTapGestureRecognizer.Tapped += NextButton_Clicked;
            bottomView.NextButton.GestureRecognizers.Add(nextTapGestureRecognizer);

            bottomView.PreviousButton.IsVisible = true;
            var backTapGestureRecognizer = new TapGestureRecognizer();
            backTapGestureRecognizer.Tapped += PreviousButton_Clicked;
            bottomView.PreviousButton.GestureRecognizers.Add(backTapGestureRecognizer);
        }

        public async void CameraBtn_Tapped(object sender, EventArgs e)
        {
            try
            {
                using (var scope = new ActivityIndicatorScope(activityIndicator, activityIndicatorPanel, true))
                {
                    IPlatform platform = DependencyService.Get<IPlatform>();
                    string sourceImagePath = await platform.TakePhotoAsync(App.UIContext);

                    if (sourceImagePath != null)
                    {
                        bool bCreateImage = this.fileName.Contains("{Correlation Id}");

                        this.fileName = this.fileName.Replace("{Correlation Id}", this.claimViewModel.Claim.Id);
                        string copiedFilePath = await FileHelper.CopyFileAsync(this.claimViewModel.Claim.Id, fileName, sourceImagePath, MobileServiceHelper.msInstance.DataFilesPath);

                        if (bCreateImage || claimViewModel.getKindImagesFileCount(type) == 0)
                        {
                            await claimViewModel.AddNewClaimFileAsync(fileName, copiedFilePath);
                        }
                        //replace only refresh
                        claimViewModel.PropertyChangeImages();
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.TraceException("Image upload failed. ", ex);
                await DisplayAlert("Image upload failed", "Image upload failed. Please try again later", "Ok");
            }
        }

        #region User Common Actions

        private async void PreviousButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(true);
            return;
        }

        private async void NextButton_Clicked(object sender, EventArgs e)
        {
            if (this.fileName.Contains("{Correlation Id}")) return;

            if (type == ClaimImageTypeModel.LicensePlate
                || type == ClaimImageTypeModel.InsuranceCard)
            {
                var nextPage = new PartyInfoView(claimViewModel, type + 1);
                NavigationPage.SetHasBackButton(nextPage, false);
                await Navigation.PushAsync(nextPage, true);
            }
            else if (type == ClaimImageTypeModel.DriversLicense)
            {
                var nextPage = new PartyContactView(claimViewModel);
                NavigationPage.SetHasBackButton(nextPage, false);
                await Navigation.PushAsync(nextPage, true);
            }
        }

        public async void MenuClicked(object sender, EventArgs e)
        {
            this.menuList.IsVisible = !this.menuList.IsVisible;
        }

        public async void LogoutBtn_Tapped(object sender, EventArgs e)
        {
            this.menuList.IsVisible = false;
            await MobileServiceHelper.msInstance.DoLogOutAsync();
            await Navigation.PopToRootAsync(true);
        }

        public async void SettingsBtn_Tapped(object sender, EventArgs e)
        {
            this.menuList.IsVisible = false;
            var settingsView = new SettingsView();
            NavigationPage.SetHasBackButton(settingsView, false);
            await Navigation.PushAsync(settingsView, false);
        }

        #endregion
    }
}
