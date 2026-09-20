using System;
using Microsoft.Maui.Controls;

namespace ApotekApp.Components;

public partial class ModalLayout : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(ModalLayout), default(string));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public event EventHandler? BatalClicked;
    public event EventHandler? SimpanClicked;

    public ModalLayout()
    {
        InitializeComponent();
    }

    private void OnBatalClicked(object sender, EventArgs e) => BatalClicked?.Invoke(this, e);
    private void OnSimpanClicked(object sender, EventArgs e) => SimpanClicked?.Invoke(this, e);
}