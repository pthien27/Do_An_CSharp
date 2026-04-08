using System;
using System.Linq;
using Microsoft.Maui.Controls;
using ZXing.Net.Maui;

namespace VinhKhanhstreet.Pages;

public partial class QrScannerPage : ContentPage
{
    public Action<string> OnScanResult { get; set; }
    private bool _isProcessing = false;

    public QrScannerPage()
    {
        InitializeComponent();

        barcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        barcodeReader.IsDetecting = true;
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
        barcodeReader.IsDetecting = false;
    }

    private void CameraBarcodeReaderView_BarcodesDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        if (_isProcessing || e.Results == null || !e.Results.Any()) return;

        var first = e.Results.FirstOrDefault();
        if (first != null && !string.IsNullOrEmpty(first.Value))
        {
            _isProcessing = true;
            barcodeReader.IsDetecting = false;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Trả kết quả chữ (thường là tên Quán) về cho MapPage
                OnScanResult?.Invoke(first.Value);
                await Navigation.PopModalAsync();
            });
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        barcodeReader.IsDetecting = false;
        await Navigation.PopModalAsync();
    }
}
