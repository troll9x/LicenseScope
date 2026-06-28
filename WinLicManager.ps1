# WinLicManager.ps1
# Windows Licensing & Information Manager
# Created for managing Windows 10/11 version info, license checking, key testing, key removal, and activation resets.
# Automatically requests administrator privileges if run in a non-admin terminal.
# Verbose mode: Prints the exact system command/arguments being executed for each action.
# Custom Masking: Asks user whether to display full or partial keys.
# Combined Key Inspection: Displays active keys, backup keys, and identifies digital entitlement.

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

# Self-elevation logic
if (-not $isAdmin) {
    Write-Host "[*] Script is not running as Administrator. Requesting elevation..." -ForegroundColor Yellow
    try {
        # Relaunch the script with RunAs verb to trigger User Account Control (UAC) elevation
        Start-Process powershell.exe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
        Exit
    } catch {
        Write-Host "[-] Failed to elevate privileges or UAC request was denied: $_" -ForegroundColor Red
        Write-Host "[-] Please run this script in an elevated Administrator PowerShell prompt manually." -ForegroundColor Red
        $confirm = Read-Host "Would you like to run in Read-Only mode anyway? (y/n)"
        if ($confirm -notmatch "^y(es)?$") {
            Exit
        }
    }
}

$slmgrPath = Join-Path $env:SystemRoot "System32\slmgr.vbs"
$genericKeys = @{
    "3V66T" = "Windows 10/11 Pro Generic Key (VK7JG-NPHTM-C97JM-9MPGT-3V66T)"
    "3TTL4" = "Windows 10/11 Home Generic Key (YTMG3-N6KGA-8B33D-XXYF2-3TTL4)"
    "WXCHW" = "Windows 10/11 Home Single Language Generic Key (4CPRK-NM3K3-X6XXQ-RXX86-WXCHW)"
    "PR4Y7" = "Windows Pro Education Generic Key (8PTT6-RNW4C-X6V77-D23ST-PR4Y7)"
    "2YV77" = "Windows Pro Workstations Generic Key (DXG7C-N36C4-C4HTG-X4T3X-2YV77)"
    "8DEC2" = "Windows Enterprise Generic Key (XGVPP-NMH47-7TTHJ-W3FW7-8DEC2)"
    "28UTV" = "Windows Enterprise Generic Key (NPPR9-FWDCX-D2C8J-H8P65-28UTV)"
    "7CFBY" = "Windows Education Generic Key (YNMGQ-8RYV3-4PGQ3-C8XTP-7CFBY)"
}

function Show-Header {
    Clear-Host
    Write-Host "=========================================================" -ForegroundColor Cyan
    Write-Host "       Windows Licensing & Information Utility           " -ForegroundColor Green
    Write-Host "=========================================================" -ForegroundColor Cyan
    if ($isAdmin) {
        Write-Host "[STATUS] Running with Administrator Privileges" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Running without Administrator Privileges!" -ForegroundColor Yellow
        Write-Host "          (License modifications and reset tasks will fail)" -ForegroundColor Yellow
    }
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
}

function Show-Menu {
    Write-Host "1. View Windows Version & Firmware (BIOS) Key Details"
    Write-Host "2. Check License Channel & Type (slmgr /dli) [Natively shows partial key only]"
    Write-Host "3. View Active & Backup Keys, Detailed Licensing Info (WMI/slmgr)"
    Write-Host "4. Test & Apply a New Product Key (Requires Admin)" -ForegroundColor Yellow
    Write-Host "5. Uninstall Current Product Key & License (Requires Admin)" -ForegroundColor Yellow
    Write-Host "6. Reset Licensing Status / Rearm (Requires Admin)" -ForegroundColor Yellow
    Write-Host "7. Exit"
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
}

function Get-VersionInfo {
    Write-Host "[VERBOSE] Command: Get-CimInstance Win32_OperatingSystem | Select-Object Caption, Version, BuildNumber, OSArchitecture" -ForegroundColor DarkGray
    Write-Host "[VERBOSE] Command: (Get-CimInstance -ClassName SoftwareLicensingService).OA3xOriginalProductKey" -ForegroundColor DarkGray
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    Write-Host "[*] Fetching Windows OS and hardware license details..." -ForegroundColor Cyan
    try {
        $os = Get-CimInstance Win32_OperatingSystem
        Write-Host "---------------------------------------------------------" -ForegroundColor Gray
        Write-Host "OS Caption:       " -NoNewline; Write-Host $os.Caption -ForegroundColor Green
        Write-Host "Version:          " -NoNewline; Write-Host $os.Version -ForegroundColor Green
        Write-Host "Build Number:     " -NoNewline; Write-Host $os.BuildNumber -ForegroundColor Green
        Write-Host "Architecture:     " -NoNewline; Write-Host $os.OSArchitecture -ForegroundColor Green
        
        # Retrieve OEM original product key from motherboard firmware (BIOS/UEFI)
        $oemKey = (Get-CimInstance -ClassName SoftwareLicensingService -ErrorAction SilentlyContinue).OA3xOriginalProductKey
        if ($oemKey) {
            $showFull = Read-Host "BIOS OEM Key detected. Show full key? (y/n)"
            if ($showFull -eq 'y' -or $showFull -eq 'yes') {
                Write-Host "BIOS OEM Key:     " -NoNewline; Write-Host $oemKey -ForegroundColor Green
            } else {
                $maskedOemKey = "XXXXX-XXXXX-XXXXX-XXXXX-" + $oemKey.Split('-')[-1]
                Write-Host "BIOS OEM Key:     " -NoNewline; Write-Host $maskedOemKey -ForegroundColor Green
            }
        } else {
            Write-Host "BIOS OEM Key:     " -NoNewline; Write-Host "None detected in BIOS/UEFI firmware" -ForegroundColor Yellow
        }
        Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    } catch {
        Write-Host "[-] Failed to fetch OS details using CIM instance." -ForegroundColor Red
        Write-Host "[VERBOSE] Command fallback: Start-Process 'winver'" -ForegroundColor DarkGray
        Write-Host "[*] Running fallback (winver)..." -ForegroundColor Yellow
        Start-Process "winver"
    }
}

function Run-SlmgrCommand {
    param(
        [string]$option
    )
    
    if (-not (Test-Path $slmgrPath)) {
        Write-Host "[-] Error: slmgr.vbs not found at $slmgrPath" -ForegroundColor Red
        return
    }

    Write-Host "[VERBOSE] Command: cscript //nologo `"$slmgrPath`" $option" -ForegroundColor DarkGray
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    Write-Host "[NOTE] Windows stores active keys securely. slmgr commands natively only reveal the partial key (last 5 chars)." -ForegroundColor Yellow
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    Write-Host "[*] Querying Windows Software Licensing service..." -ForegroundColor Cyan
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    
    # Run using cscript to redirect stdout to the console instead of showing popup dialogs
    $output = cscript //nologo $slmgrPath $option 2>&1
    
    # If cscript fails or produces no output, fall back to wscript/direct execution
    if ($null -eq $output -or $output.Length -eq 0) {
        Write-Host "[VERBOSE] Command Fallback: Start-Process 'cscript.exe' '//nologo `"$slmgrPath`" $option'" -ForegroundColor DarkGray
        Write-Host "[*] Console output empty. Spawning GUI dialog instead..." -ForegroundColor Yellow
        Start-Process -FilePath "cscript.exe" -ArgumentList "//nologo `"$slmgrPath`" $option" -NoNewWindow -Wait
    } else {
        foreach ($line in $output) {
            if ($line -match "Description:") {
                Write-Host $line -ForegroundColor Green
            } elseif ($line -match "License Status:") {
                Write-Host $line -ForegroundColor Cyan
            } else {
                Write-Host $line
            }
        }
    }
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
}

function Inspect-KeysAndLicenses {
    Write-Host "[VERBOSE] Command: Get-CimInstance -ClassName SoftwareLicensingProduct" -ForegroundColor DarkGray
    Write-Host "[VERBOSE] Command: Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform'" -ForegroundColor DarkGray
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    Write-Host "[*] Fetching active keys and licensing details..." -ForegroundColor Cyan
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray

    # 1. Fetch Active License details via WMI
    $activeProduct = Get-CimInstance -ClassName SoftwareLicensingProduct | Where-Object { $_.PartialProductKey -and $_.Name -like "Windows*" }
    $partialKey = $null
    
    if ($activeProduct) {
        $partialKey = $activeProduct.PartialProductKey
        try {
            $os = Get-CimInstance Win32_OperatingSystem
            Write-Host "Installed OS Edition:   " -NoNewline; Write-Host $os.Caption -ForegroundColor Green
        } catch {}
        Write-Host "Active License Name:    " -NoNewline; Write-Host $activeProduct.Name -ForegroundColor Green
        Write-Host "Active License Channel: " -NoNewline; Write-Host $activeProduct.Description -ForegroundColor Green
        Write-Host "Active Partial Key:     " -NoNewline; Write-Host $partialKey -ForegroundColor Green
        
        $statusText = switch ($activeProduct.LicenseStatus) {
            0 { "Unlicensed" }
            1 { "Licensed (Permanently Activated)" }
            2 { "OOB Grace Period" }
            3 { "OOT Grace Period" }
            4 { "Non-Genuine Grace Period" }
            5 { "Notification Mode" }
            6 { "Extended Grace Period" }
            default { "Unknown Status" }
        }
        Write-Host "Activation Status:      " -NoNewline
        if ($activeProduct.LicenseStatus -eq 1) {
            Write-Host $statusText -ForegroundColor Green
        } else {
            Write-Host $statusText -ForegroundColor Red
        }

        # Check if the active key is a known generic digital license key
        if ($genericKeys.ContainsKey($partialKey)) {
            Write-Host "`n[INFO] Activation Method: Digital License (Digital Entitlement)" -ForegroundColor Green
            Write-Host "       The active key matches a generic installation key: " -NoNewline
            Write-Host $genericKeys[$partialKey] -ForegroundColor Cyan
            Write-Host "       This indicates Windows is activated via a Digital License linked to your device hardware." -ForegroundColor Cyan
            Write-Host "       Digital licenses are typically tied to your Microsoft Account (MSA)." -ForegroundColor Cyan
            Write-Host "       To verify MSA linkage, visit: Settings > System > Activation" -ForegroundColor Yellow
        }
    } else {
        Write-Host "[-] Active Windows product license details not found via WMI." -ForegroundColor Red
    }

    Write-Host "---------------------------------------------------------" -ForegroundColor Gray

    # 2. Fetch BIOS OEM key
    $oemKey = (Get-CimInstance -ClassName SoftwareLicensingService -ErrorAction SilentlyContinue).OA3xOriginalProductKey
    if ($oemKey) {
        Write-Host "Firmware BIOS OEM Key:  " -NoNewline; Write-Host "Detected" -ForegroundColor Green
    } else {
        Write-Host "Firmware BIOS OEM Key:  " -NoNewline; Write-Host "None detected" -ForegroundColor Yellow
    }

    # 3. Fetch Registry Backup Key
    $regKeyPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform"
    $regKey = (Get-ItemProperty -Path $regKeyPath -Name "BackupProductKeyDefault" -ErrorAction SilentlyContinue).BackupProductKeyDefault
    
    if ($regKey) {
        Write-Host "Registry Backup Key:    " -NoNewline; Write-Host "Detected" -ForegroundColor Green
    } else {
        Write-Host "Registry Backup Key:    " -NoNewline; Write-Host "None found" -ForegroundColor Yellow
    }

    Write-Host "---------------------------------------------------------" -ForegroundColor Gray

    # Ask how to display keys if detected
    if ($oemKey -or $regKey) {
        $showFull = Read-Host "Show full product keys in this display? (y/n)"
        $displayFull = ($showFull -eq 'y' -or $showFull -eq 'yes')
        
        Write-Host "`n--- KEY DETAILS ---" -ForegroundColor Cyan
        if ($oemKey) {
            $val = if ($displayFull) { $oemKey } else { "XXXXX-XXXXX-XXXXX-XXXXX-" + $oemKey.Split('-')[-1] }
            Write-Host "BIOS OEM Key:        $val" -ForegroundColor Green
        }
        if ($regKey) {
            $val = if ($displayFull) { $regKey } else { "XXXXX-XXXXX-XXXXX-XXXXX-" + $regKey.Split('-')[-1] }
            Write-Host "Registry Backup Key: $val" -ForegroundColor Green
        }
        Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    }

    # 4. Compare Registry Backup Key with Active Key and BIOS Key
    if ($regKey -and $partialKey) {
        $regKeyEndsWithActive = $regKey.EndsWith($partialKey)
        
        if (-not $regKeyEndsWithActive) {
            Write-Host "[WARNING] Your Registry Backup Key does not match your active license!" -ForegroundColor Yellow
            $maskedReg = if ($regKey) { "XXXXX-XXXXX-XXXXX-XXXXX-" + $regKey.Split('-')[-1] } else { "None" }
            Write-Host "          - Registry Backup Key: " -NoNewline; Write-Host $maskedReg -ForegroundColor Yellow
            Write-Host "          - Active Partial Key:  " -NoNewline; Write-Host $partialKey -ForegroundColor Yellow
            Write-Host "          This means you upgraded editions or are activating via a different key/method." -ForegroundColor Cyan
            Write-Host "          You can safely remove the backup key registry entry if you want to clean up." -ForegroundColor Cyan
            Write-Host "---------------------------------------------------------" -ForegroundColor Gray

            if ($isAdmin) {
                $removeReg = Read-Host "Would you like to remove the Registry Backup Key only? (Your active status will NOT be affected) (y/n)"
                if ($removeReg -eq 'yes' -or $removeReg -eq 'y') {
                    Write-Host "[VERBOSE] Command: Remove-ItemProperty -Path `"$regKeyPath`" -Name `"BackupProductKeyDefault`" -Force" -ForegroundColor DarkGray
                    try {
                        Remove-ItemProperty -Path $regKeyPath -Name "BackupProductKeyDefault" -Force
                        Write-Host "[+] Registry backup key successfully removed." -ForegroundColor Green
                    } catch {
                        Write-Host "[-] Failed to remove registry key: $_" -ForegroundColor Red
                    }
                } else {
                    Write-Host "[*] Registry backup key was kept." -ForegroundColor Yellow
                }
            } else {
                Write-Host "[-] Run as Administrator to unlock the option to delete the backup key." -ForegroundColor Red
            }
        } else {
            Write-Host "[+] Your Registry Backup Key matches your currently active product key." -ForegroundColor Green
        }
        Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    }

    # Optional detailed slmgr report
    $showDlv = Read-Host "Would you like to run the full detailed slmgr /dlv report now? (y/n)"
    if ($showDlv -eq 'yes' -or $showDlv -eq 'y') {
        Run-SlmgrCommand "/dlv"
    }
}

function Test-ProductKey {
    if (-not $isAdmin) {
        Write-Host "[-] Error: Administrator privileges are required to test or apply a product key." -ForegroundColor Red
        return
    }

    Write-Host "[INFO] Windows product key checks are performed by attempting local installation." -ForegroundColor Cyan
    Write-Host "       - If the key is valid and matches the SKU, it will be installed successfully." -ForegroundColor Cyan
    Write-Host "       - If it is incompatible (SKU mismatch) or invalid, it will fail with an error code." -ForegroundColor Cyan
    Write-Host "       - Your existing key will NOT be blocked or harmed by testing a mismatched key." -ForegroundColor Cyan
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray

    $rawKey = Read-Host "Enter the 25-character product key (XXXXX-XXXXX-XXXXX-XXXXX-XXXXX)"
    $key = $rawKey.Trim().ToUpper()

    # Simple validation check (25 characters with dashes)
    if ($key -notmatch "^[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}$") {
        Write-Host "[-] Error: Invalid format. Key must be 25 alphanumeric characters split by dashes." -ForegroundColor Red
        return
    }

    # Ask the user if they want to display the full key or partial key in the verbose command output
    $showFullVerbose = Read-Host "Show full product key in the verbose command log? (y/n)"
    if ($showFullVerbose -eq 'y' -or $showFullVerbose -eq 'yes') {
        $displayKey = $key
    } else {
        $displayKey = "XXXXX-XXXXX-XXXXX-XXXXX-" + $key.Split('-')[-1]
    }

    Write-Host "[VERBOSE] Command: cscript //nologo `"$slmgrPath`" /ipk $displayKey" -ForegroundColor DarkGray
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    
    Write-Host "[*] Attempting to install and check product key..." -ForegroundColor Cyan
    $output = cscript //nologo $slmgrPath /ipk $key 2>&1

    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    
    $success = $true
    foreach ($line in $output) {
        Write-Host $line
        if ($line -match "Error:" -or $line -match "0x") {
            $success = $false
        }
    }

    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    if ($success) {
        Write-Host "[+] SUCCESS: The product key was successfully installed and accepted!" -ForegroundColor Green
        Write-Host "[*] Windows will now attempt to activate online automatically. Check activation in Option 2." -ForegroundColor Green
    } else {
        Write-Host "[-] FAILED: The key was rejected." -ForegroundColor Red
        
        # Analyze output for known Windows error codes
        $fullOutputString = [string]::Join(" ", $output)
        $errorCode = ""
        if ($fullOutputString -match "(0x[A-Fa-f0-9]+)") {
            $errorCode = $Matches[1]
        }
        
        if ($fullOutputString -match "0xC004F069") {
            Write-Host "[-] Diagnosis: SKU Mismatch (Error 0xC004F069)." -ForegroundColor Yellow
            Write-Host "    The product key belongs to a different Windows edition (e.g. Home vs. Pro) than installed." -ForegroundColor Yellow
        } elseif ($fullOutputString -match "0xC004F050") {
            Write-Host "[-] Diagnosis: Invalid Product Key (Error 0xC004F050)." -ForegroundColor Yellow
            Write-Host "    The key is either invalid, mistyped, or not recognized by this Windows version." -ForegroundColor Yellow
        } elseif ($fullOutputString -match "0xC004C003") {
            Write-Host "[-] Diagnosis: Blocked Key (Error 0xC004C003)." -ForegroundColor Yellow
            Write-Host "    The product key has been blocked or blacklisted by Microsoft's activation servers." -ForegroundColor Yellow
        } else {
            Write-Host "[-] Diagnosis: Installation failed. Please check the error code online." -ForegroundColor Yellow
        }

        # Print support and validation URLs
        Write-Host "`n[SUPPORT & VALIDATION HELP]" -ForegroundColor Cyan
        Write-Host "- Windows Activation Help URL:  https://support.microsoft.com/help/10738" -ForegroundColor Green
        if ($errorCode) {
            Write-Host "- Error Code Troubleshoot URL:  https://learn.microsoft.com/en-us/windows-server/get-started/activation-error-codes" -ForegroundColor Green
            Write-Host "- Search Google for Error Info: https://www.google.com/search?q=Windows+activation+error+$errorCode" -ForegroundColor Green
        } else {
            Write-Host "- Error Code Reference URL:     https://learn.microsoft.com/en-us/windows-server/get-started/activation-error-codes" -ForegroundColor Green
        }
    }
}

function Remove-License {
    if (-not $isAdmin) {
        Write-Host "[-] Error: Administrator privileges are required to uninstall the product key." -ForegroundColor Red
        return
    }
    
    Write-Host "[WARNING] This action will uninstall the current Windows product key" -ForegroundColor Red
    Write-Host "          and clear it from the registry. Your Windows will become unactivated." -ForegroundColor Red
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    
    $confirm = Read-Host "Are you sure you want to proceed? (y/n)"
    if ($confirm -eq 'yes' -or $confirm -eq 'y') {
        Write-Host "[VERBOSE] Command: cscript //nologo `"$slmgrPath`" /upk" -ForegroundColor DarkGray
        Write-Host "[VERBOSE] Command: cscript //nologo `"$slmgrPath`" /cpky" -ForegroundColor DarkGray
        Write-Host "---------------------------------------------------------" -ForegroundColor Gray
        
        Write-Host "[*] Uninstalling product key (slmgr /upk)..." -ForegroundColor Cyan
        $upkOut = cscript //nologo $slmgrPath /upk 2>&1
        Write-Host $upkOut
        
        Write-Host "[*] Clearing product key from registry (slmgr /cpky)..." -ForegroundColor Cyan
        $cpkyOut = cscript //nologo $slmgrPath /cpky 2>&1
        Write-Host $cpkyOut
        
        Write-Host "[+] Product key removed and registry cleared successfully." -ForegroundColor Green
    } else {
        Write-Host "[*] Operation cancelled." -ForegroundColor Yellow
    }
}

function Reset-Activation {
    if (-not $isAdmin) {
        Write-Host "[-] Error: Administrator privileges are required to reset licensing status." -ForegroundColor Red
        return
    }
    
    Write-Host "[WARNING] This will reset the licensing status and activation timers (rearm)." -ForegroundColor Red
    Write-Host "          A computer restart is required for changes to take effect." -ForegroundColor Red
    Write-Host "---------------------------------------------------------" -ForegroundColor Gray
    
    $confirm = Read-Host "Are you sure you want to proceed? (y/n)"
    if ($confirm -eq 'yes' -or $confirm -eq 'y') {
        Write-Host "[VERBOSE] Command: cscript //nologo `"$slmgrPath`" /rearm" -ForegroundColor DarkGray
        Write-Host "---------------------------------------------------------" -ForegroundColor Gray
        
        Write-Host "[*] Resetting licensing status (slmgr /rearm)..." -ForegroundColor Cyan
        $rearmOut = cscript //nologo $slmgrPath /rearm 2>&1
        Write-Host $rearmOut
        Write-Host "[+] Rearm completed. Please restart your computer for changes to take effect." -ForegroundColor Green
        
        $restart = Read-Host "Would you like to restart the computer now? (y/n)"
        if ($restart -eq 'yes' -or $restart -eq 'y') {
            Write-Host "[VERBOSE] Command: Restart-Computer" -ForegroundColor DarkGray
            Write-Host "[*] Restarting computer..." -ForegroundColor Yellow
            Restart-Computer
        }
    } else {
        Write-Host "[*] Operation cancelled." -ForegroundColor Yellow
    }
}

# Main loop
do {
    Show-Header
    Show-Menu
    $choice = Read-Host "Please select an option (1-7)"
    
    switch ($choice) {
        "1" {
            Show-Header
            Get-VersionInfo
            Read-Host "Press Enter to return to the menu..."
        }
        "2" {
            Show-Header
            Run-SlmgrCommand "/dli"
            Read-Host "Press Enter to return to the menu..."
        }
        "3" {
            Show-Header
            Inspect-KeysAndLicenses
            Read-Host "Press Enter to return to the menu..."
        }
        "4" {
            Show-Header
            Test-ProductKey
            Read-Host "Press Enter to return to the menu..."
        }
        "5" {
            Show-Header
            Remove-License
            Read-Host "Press Enter to return to the menu..."
        }
        "6" {
            Show-Header
            Reset-Activation
            Read-Host "Press Enter to return to the menu..."
        }
        "7" {
            Write-Host "[*] Exiting. Have a great day!" -ForegroundColor Green
            break
        }
        default {
            Write-Host "[-] Invalid option, please choose between 1 and 7." -ForegroundColor Red
            Start-Sleep -Seconds 1.5
        }
    }
} while ($choice -ne "7")
