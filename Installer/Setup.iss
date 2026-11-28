[Setup]
AppId={{A6D0CBF4-1E65-4D24-9A7F-3A9E0D1F2B53}}
AppName=Deadzone2D
AppPublisher=Lume Studio INC
SetupIconFile=..\DeadzoneWin\App\Assets\DeadzoneIcon.ico

AppVersion=4.5.1
AppComments=Published 2026-11-26 03:46 - Deadzone2D
AppCopyright=Lume Studio INC
DefaultDirName={commonpf}\Deadzone
DefaultGroupName=Deadzone
DisableWelcomePage=yes
DisableDirPage=no
DisableProgramGroupPage=yes
PrivilegesRequired=admin
LicenseFile=LICENSE.md
OutputDir=..\dist
OutputBaseFilename=DeadzoneSetup_v4.5.1
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
WizardResizable=no
; Imagem lateral (tela final) em vários tamanhos para telas com escala de DPI
WizardImageFile=assets\side_164x314.bmp,assets\side_192x386.bmp,assets\side_246x459.bmp,assets\side_273x556.bmp,assets\side_328x604.bmp,assets\side_355x632.bmp,assets\side_410x797.bmp

[Messages]
WizardPreparing=Preparando a instalação do Deadzone...

[Tasks]
Name: "update"; Description: "Updates — check for a newer version and install it"; GroupDescription: "Game utilities:"

[Languages]
Name: english; MessagesFile: compiler:Default.isl

[Files]
Source: "assets\header.bmp"; Flags: dontcopy
Source: "..\dist\Deadzone\*"; DestDir: {app}; Flags: ignoreversion recursesubdirs createallsubdirs

[Files]
Source: "CheckUpdate.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Deadzone"; Filename: "{app}\Deadzone2DApp.exe"; WorkingDir: "{app}"
Name: "{commondesktop}\Deadzone"; Filename: "{app}\Deadzone2DApp.exe"; IconFilename: "{app}\Deadzone2DApp.exe"

[Run]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\CheckUpdate.ps1"""; Flags: runhidden unchecked; Tasks: update
Filename: "{app}\Deadzone2DApp.exe"; Description: "Launch Deadzone"; Flags: postinstall nowait runasoriginaluser

[Code]
// ---------- Paleta (formato BGR: $00BBGGRR) ----------
const
  DzBg     = $090610;   // #100609  fundo vinho-preto
  DzPanel  = $110B1D;   // #1D0B11  cabeçalho / caixas
  DzAccent = $3C28E8;   // #E8283C  vermelho carmim
  DzText   = $EEECF5;   // #F5ECEE  texto
  DzMuted  = $AEA8C2;   // #C2A8AE  texto secundário

  DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
  DWMWA_BORDER_COLOR  = 34;   // Windows 11
  DWMWA_CAPTION_COLOR = 35;   // Windows 11
  DWMWA_TEXT_COLOR    = 36;   // Windows 11

function DwmSetWindowAttribute(hwnd: HWND; dwAttribute: Cardinal;
  var pvAttribute: Cardinal; cbAttribute: Cardinal): Integer;
  external 'DwmSetWindowAttribute@dwmapi.dll stdcall delayload';

function SetWindowTheme(hwnd: HWND; pszSubAppName: String;
  pszSubIdList: String): Integer;
  external 'SetWindowTheme@uxtheme.dll stdcall delayload';

procedure SetDwm(Attr, Value: Cardinal);
begin
  try
    DwmSetWindowAttribute(WizardForm.Handle, Attr, Value, 4);
  except
    // Windows antigo sem DWM: ignora
  end;
end;

procedure ThemeControl(C: TControl);
var
  i: Integer;
begin
  if C is TNewStaticText then
    TNewStaticText(C).Font.Color := DzText
  else if C is TNewCheckBox then
    TNewCheckBox(C).Font.Color := DzText
  else if C is TNewRadioButton then
    TNewRadioButton(C).Font.Color := DzText
  else if C is TNewCheckListBox then
  begin
    TNewCheckListBox(C).Color := DzBg;
    TNewCheckListBox(C).Font.Color := DzText;
  end
  else if C is TRichEditViewer then
  begin
    TRichEditViewer(C).Color := DzPanel;
    TRichEditViewer(C).Font.Color := DzText;
  end
  else if C is TEdit then
  begin
    TEdit(C).Color := DzPanel;
    TEdit(C).Font.Color := DzText;
  end
  else if C is TPanel then
  begin
    TPanel(C).ParentBackground := False;
    TPanel(C).Color := DzBg;
  end
  else if C is TNewNotebookPage then
    TNewNotebookPage(C).Color := DzBg;

  if C is TWinControl then
    for i := 0 to TWinControl(C).ControlCount - 1 do
      ThemeControl(TWinControl(C).Controls[i]);
end;

procedure InitializeWizard;
var
  Banner: TBitmapImage;
  RedLine: TPanel;
begin
  // Só botão de fechar: sem minimizar/maximizar e sem redimensionar
  WizardForm.BorderStyle := bsDialog;
  WizardForm.BorderIcons := [biSystemMenu];

  // Barra de título e borda pretas (Windows 10 20H1+ / Windows 11)
  SetDwm(DWMWA_USE_IMMERSIVE_DARK_MODE, 1);
  SetDwm(DWMWA_CAPTION_COLOR, $000000);
  SetDwm(DWMWA_BORDER_COLOR, $000000);
  SetDwm(DWMWA_TEXT_COLOR, $FFFFFF);

  // Fundo e textos de todas as páginas
  WizardForm.Color := DzBg;
  ThemeControl(WizardForm);
  WizardForm.MainPanel.Color := DzPanel;

  // Cabeçalho com o logo DEADZONE (imagem com degradê vinho e vermelho)
  WizardForm.WizardSmallBitmapImage.Visible := False;
  ExtractTemporaryFile('header.bmp');
  Banner := TBitmapImage.Create(WizardForm);
  Banner.Parent := WizardForm.MainPanel;
  Banner.Align := alClient;
  Banner.Stretch := True;
  Banner.Bitmap.LoadFromFile(ExpandConstant('{tmp}\header.bmp'));
  Banner.SendToBack;

  // Deixa o texto do cabeçalho longe do logo
  WizardForm.PageDescriptionLabel.Width := WizardForm.MainPanel.Width * 64 div 100;
  WizardForm.PageNameLabel.Font.Color := DzAccent;
  WizardForm.PageNameLabel.Font.Style := [fsBold];
  WizardForm.PageDescriptionLabel.Font.Color := DzMuted;
  WizardForm.FinishedHeadingLabel.Font.Color := DzAccent;

  // Faixa vermelha embaixo do cabeçalho
  RedLine := TPanel.Create(WizardForm);
  RedLine.Parent := WizardForm.MainPanel;
  RedLine.Align := alBottom;
  RedLine.Height := ScaleY(3);
  RedLine.BevelOuter := bvNone;
  RedLine.ParentBackground := False;
  RedLine.Color := DzAccent;

  // Barra de progresso vermelha sobre fundo escuro
  try
    SetWindowTheme(WizardForm.ProgressGauge.Handle, '', '');
    SendMessage(WizardForm.ProgressGauge.Handle, $0409, 0, DzAccent);  // PBM_SETBARCOLOR
    SendMessage(WizardForm.ProgressGauge.Handle, $2001, 0, DzPanel);   // PBM_SETBKCOLOR
  except
  end;
end;







