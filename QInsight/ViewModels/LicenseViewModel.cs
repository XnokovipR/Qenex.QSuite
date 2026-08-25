using System.Windows;
using Qenex.QInsight.Licensing;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public class LicenseViewModel : PropertyChangedBaseWithValidation
{
    private readonly LicenseService licenseService;
    private readonly Logger logger;
    private readonly Func<int>? communicatedSignalCountProvider;
    private RadWindow? parentWindow;

    public LicenseViewModel(LicenseService licenseService, Logger logger,
        Func<int>? communicatedSignalCountProvider = null)
    {
        this.licenseService = licenseService;
        this.logger = logger;
        this.communicatedSignalCountProvider = communicatedSignalCountProvider;

        ActivateCommand = new RelayCommandAsync<object>(ActivateAsync, _ => CanActivate());
        CopyMachineCodeCommand = new RelayCommand<object>(_ => CopyMachineCode());
        ActivateOfflineCommand = new RelayCommand<object>(_ => ActivateOffline(),
            _ => !IsBusy && !string.IsNullOrWhiteSpace(OfflineCodeInput));
        CloseCommand = new RelayCommand<object>(_ => parentWindow?.Close());

        licenseService.StateChanged += OnLicenseStateChanged;
        RefreshStatus();

        // Machine deactivated in the portal (or activation failed earlier): prefill the
        // remembered key so re-activation is a single click.
        if (!IsLicensed && !string.IsNullOrWhiteSpace(licenseService.LicenseKey))
        {
            LicenseKeyInput = licenseService.LicenseKey;
        }
    }

    public string LicenseKeyInput
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            ActivateCommand.OnCanExecuteChanged();
        }
    } = string.Empty;

    public bool IsBusy
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            ActivateCommand.OnCanExecuteChanged();
            ActivateOfflineCommand.OnCanExecuteChanged();
        }
    }

    /// <summary>Machine code for offline activation on account.qenex.net — the machine
    /// fingerprint the signed activation code will be bound to.</summary>
    public string MachineCodeText => MachineFingerprint.Get();

    public string OfflineCodeInput
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            ActivateOfflineCommand.OnCanExecuteChanged();
        }
    } = string.Empty;

    public string ErrorMessage
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    } = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsLicensed
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    }

    /// <summary>True when claims of a (possibly expired) license are known and can be shown.</summary>
    public bool HasLicenseInfo
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    }

    public bool IsFreeTier
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    } = string.Empty;

    public string TierText
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    } = string.Empty;

    public string SubscriptionEndsText
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    } = string.Empty;

    public string TokenValidUntilText
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    } = string.Empty;

    public string MachineText => Environment.MachineName;

    /// <summary>True when the Free-tier communicated-signal counter should be shown.</summary>
    public bool ShowCommunicatedSignals
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    }

    public string CommunicatedSignalsText
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    } = string.Empty;

    public RelayCommandAsync<object> ActivateCommand { get; }
    public RelayCommand<object> CopyMachineCodeCommand { get; }
    public RelayCommand<object> ActivateOfflineCommand { get; }
    public RelayCommand<object> CloseCommand { get; }

    public void SetParentWindow(RadWindow window)
    {
        parentWindow = window;
        window.Closed += (_, _) => licenseService.StateChanged -= OnLicenseStateChanged;
    }

    private bool CanActivate()
    {
        return !IsBusy && LicenseKey.IsValidFormat(NormalizeKeyInput());
    }

    private string NormalizeKeyInput() => LicenseKeyInput.Trim().ToUpperInvariant();

    private async Task ActivateAsync(object obj)
    {
        var licenseKey = NormalizeKeyInput();
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await licenseService.ActivateAsync(licenseKey);
            if (result.Success)
            {
                LicenseKeyInput = string.Empty;
                RefreshStatus();
            }
            else
            {
                ErrorMessage = MapError(result.Error);
                logger.Log(LogLevel.Warn, $"License activation failed: {result.Error}.");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CopyMachineCode()
    {
        try
        {
            Clipboard.SetText(MachineCodeText);
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Warn, $"Could not copy the machine code to the clipboard: {e.Message}");
        }
    }

    private void ActivateOffline()
    {
        ErrorMessage = string.Empty;
        var status = licenseService.ActivateOffline(OfflineCodeInput);
        if (status == LicenseStatus.Valid)
        {
            OfflineCodeInput = string.Empty;
            RefreshStatus();
        }
        else
        {
            ErrorMessage = status switch
            {
                LicenseStatus.Expired => "The activation code has expired. Request a new one at account.qenex.net.",
                _ => "The activation code is not valid for this machine. Copy the machine code again and request "
                     + "a new activation code at account.qenex.net."
            };
        }
    }

    private void OnLicenseStateChanged(object? sender, EventArgs e)
    {
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var claims = licenseService.CurrentClaims;

        IsLicensed = licenseService.Status == LicenseStatus.Valid;
        HasLicenseInfo = claims is not null;
        IsFreeTier = licenseService.IsFreeTier;

        StatusText = licenseService.Status switch
        {
            LicenseStatus.Valid => "Licensed",
            LicenseStatus.Expired => "License expired",
            LicenseStatus.Invalid => "License invalid",
            _ => "No license"
        };

        TierText = claims?.Tier ?? string.Empty;
        SubscriptionEndsText = claims?.SubscriptionEndsUtc is { } subscriptionEnd
            ? subscriptionEnd.ToLocalTime().ToString("d")
            : string.Empty;
        TokenValidUntilText = claims is not null
            ? $"{claims.ExpiresAtUtc.ToLocalTime():g} (renewed automatically while online)"
            : string.Empty;

        ShowCommunicatedSignals = communicatedSignalCountProvider is not null
                                  && CommunicatedSignals.GetLimit(licenseService) is not null;
        CommunicatedSignalsText = ShowCommunicatedSignals
            ? $"{communicatedSignalCountProvider!()} / {CommunicatedSignals.GetLimit(licenseService)}"
            : string.Empty;
    }

    private static string MapError(LicenseServerError error) => error switch
    {
        LicenseServerError.UnknownLicense => "License key not found.",
        LicenseServerError.LicenseRevoked => "This license has been revoked.",
        LicenseServerError.LicenseExpired => "This license has expired.",
        LicenseServerError.LicenseNotYetValid => "This license is not valid yet.",
        LicenseServerError.NoSeatAvailable => "All seats for this license are already in use. Deactivate a machine in the account portal first.",
        LicenseServerError.NetworkError => "Could not reach the license server. Check your internet connection and try again.",
        _ => "License activation failed. Please try again later."
    };
}
