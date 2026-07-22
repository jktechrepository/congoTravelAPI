# Script PowerShell pour attribuer manuellement l'adresse IP à la carte Wi-Fi

# Nom de la carte Wi-Fi
$wifiName = "Wi-Fi"

# Adresse IP à attribuer (actuelle)
$ipAddress = "192.168.43.139"

# Masque de sous-réseau
$subnetMask = "255.255.255.0"

# Passerelle par défaut
$gateway = "192.168.43.4"

# Serveur DNS principal (actuel)
$dns1 = "192.168.43.4"

# Attribution de l'adresse IP, du masque et de la passerelle
netsh interface ip set address name="$wifiName" static $ipAddress $subnetMask $gateway

# Attribution du DNS principal
netsh interface ip set dns name="$wifiName" static $dns1

Write-Host "Configuration terminée pour la carte $wifiName avec l'adresse IP $ipAddress" 