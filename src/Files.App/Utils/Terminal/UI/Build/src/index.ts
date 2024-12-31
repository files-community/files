import { Terminal, ITerminalOptions, ILinkMatcherOptions, FontWeight } from 'xterm';
import { FitAddon } from 'xterm-addon-fit';
import { SearchAddon } from 'xterm-addon-search';
import { WebLinksAddon } from 'xterm-addon-web-links';
import { SerializeAddon } from "xterm-addon-serialize";
import { Unicode11Addon } from "xterm-addon-unicode11";

/// <summary>
/// Code modified from https://github.com/felixse/FluentTerminal
/// </summary>
interface ExtendedWindow extends Window {
  keyBindings: any[];
  term: Terminal;
  terminalBridge: any;
  hoveredUri: string;

  createTerminal(options: any, theme: any, keyBindings: any): void;
  connectToWebSocket(url: string): void;
  changeTheme(theme: any): void;
  changeOptions(options: any): void;
  changeKeyBindings(keyBindings: any): void;
  findNext(content: string, caseSensitive: boolean, wholeWord: boolean, regex: boolean): void;
  findPrevious(content: string, caseSensitive: boolean, wholeWord: boolean, regex: boolean): void;
  serializeTerminal() : void;
  setFontSize(fontSize: number): void;
  onSessionRestart(param): void;
  onOutput(data): void;
  onPaste(text): void;
}

declare var window: ExtendedWindow;

let term: Terminal;
let fitAddon: FitAddon;
let searchAddon: SearchAddon;
let serializeAddon: SerializeAddon;
let webLinksAddon: WebLinksAddon;
let unicode11Addon: Unicode11Addon;
let altGrPressed = false;
let filterOutOpenSSHOutput = false;

const terminalContainer = document.getElementById('terminal-container');

function replaceAll(searchString, replaceString, str) {
  return str.split(searchString).join(replaceString);
}

function DecodeSpecialChars(data: string) {
  data = replaceAll("&quot;", "\"", data);
  data = replaceAll("&squo;", "'", data);
  return replaceAll("&bsol;", "\\", data);
}

window.setFontSize = (fontSize: number) => {
  if (fontSize > 0) {
    term.setOption('fontSize', fontSize);
    fitAddon.fit();
  }
}

window.serializeTerminal = () => {
  let serialized = serializeAddon.serialize();
  return serialized;
}

window.createTerminal = (options, theme, keyBindings) => {
  while (terminalContainer.children.length) {
    terminalContainer.removeChild(terminalContainer.children[0]);
  }

  theme = JSON.parse(theme);
  document.body.style.background = theme.background;

  window.keyBindings = JSON.parse(keyBindings);
  window.hoveredUri = "";

  options = JSON.parse(options);

  setScrollBarStyle(options.scrollBarStyle);

  var terminalOptions: ITerminalOptions = {
    fontFamily: options.fontFamily,
    fontSize: options.fontSize,
    fontWeight: options.fontWeight,
    fontWeightBold: convertBoldText(options.fontWeight),
    cursorStyle: options.cursorStyle,
    cursorBlink: options.cursorBlink,
    bellStyle: options.bellStyle,
    scrollback: options.scrollBackLimit,
    //allowTransparency: true,
    theme: theme,
    windowsMode: true,
    wordSeparator: DecodeSpecialChars(options.wordSeparator)
  };

  term = new Terminal(terminalOptions);

  const linkMatcherOptions: ILinkMatcherOptions = {
    leaveCallback: () => {
      window.hoveredUri = "";
    },
    tooltipCallback: (event: MouseEvent, uri: string) => {
      window.hoveredUri = uri;
    }
  };

  searchAddon = new SearchAddon();
  term.loadAddon(searchAddon);
  fitAddon = new FitAddon();
  term.loadAddon(fitAddon);
  serializeAddon = new SerializeAddon();
  term.loadAddon(serializeAddon);
  webLinksAddon = new WebLinksAddon((_, u) => window.open(u), linkMatcherOptions);
  term.loadAddon(webLinksAddon);
  unicode11Addon = new Unicode11Addon();
  term.loadAddon(unicode11Addon);
  term.unicode.activeVersion = '11';

  window.term = term;

  function stringToUint8Array(param: string) {
    var str = unescape(encodeURIComponent(param));
    var charList = str.split('');
    var output = [];
    for (var i = 0; i < charList.length; i++) {
      output.push(charList[i].charCodeAt(0));
    }
    return new Uint8Array(output);
  }

  function uint8ArrayToString(param: Uint8Array) {
    var encodedString = String.fromCharCode.apply(null, param);
    return decodeURIComponent(escape(encodedString));
  }

  window.onSessionRestart = function (param: string) {
    if (filterOutOpenSSHOutput == false) {
      filterOutOpenSSHOutput = true;
    }
  }

  window.onOutput = function (data) {
    data = stringToUint8Array(data);
	
    if (filterOutOpenSSHOutput == true) {
      var str = uint8ArrayToString(data);
      var sessionStarted = str.search(/(\x1b\[H\x1b\[\?25h)|(\x1b\[2;1H\x1b\]0;OpenSSH SSH client)/u);

      if (sessionStarted != -1) {
        filterOutOpenSSHOutput = false;
      }

      // Filter out new line characters following by cursor movement CSIs X and C
      str = replaceAll(/\n\x1b\[\d*X\x1b\[\d*C/u, "", str);
      // Filter out J (clear screen) CSIs and H (set cursor position) CSIs
      str = replaceAll(/\x1b\[\d*J/u, "", str);
      // Filter out H (set cursor position) CSIs
      str = replaceAll(/\x1b\[\d*;?\d*H/u, "", str);

      data = stringToUint8Array(str);
    }

    term.writeUtf8(data);
  }

  window.onPaste = function (text) {
    term.paste(text);
  }

  term.onData(data => {
    window.terminalBridge.inputReceived(data);
  });

  term.onBinary(binary => {
    window.terminalBridge.binaryReceived(binary);
  });

  term.onResize(({ cols, rows }) => {
    window.terminalBridge.notifySizeChanged(cols, rows);
  });

  term.onTitleChange((title: string) => {
    window.terminalBridge.notifyTitleChanged(title);
  });

  term.onSelectionChange(() => {
    window.terminalBridge.notifySelectionChanged(term.getSelection());
  });

  term.open(terminalContainer);
  fitAddon.fit();
  term.focus();

  setPadding(options.padding);

  let resizeTimeout: NodeJS.Timeout;
  window.onresize = function () {
    clearTimeout(resizeTimeout);
    resizeTimeout = setTimeout(() => fitAddon.fit(), 500);
  }

  window.onmouseup = (e) => {
    if (e.button == 1) {
      window.terminalBridge.notifyMiddleClick(e.clientX, e.clientY, term.hasSelection(), window.hoveredUri);
    } else if (e.button == 2) {
      window.terminalBridge.notifyRightClick(e.clientX, e.clientY, term.hasSelection(), window.hoveredUri);
    }
  }

  window.onkeydown = (e) => {
    // Disable WebView zooming to prevent crash on too small font size
    if ((e.ctrlKey && !e.altKey && (e.keyCode === 189 || e.keyCode === 187)) ||
        (e.ctrlKey && (e.key === "Add" || e.key === "Subtract"))) {
      e.preventDefault();
    }
  }

  window.addEventListener("wheel", function(event){
    // Disable WebView zooming to prevent crash on too small font size
    if(event.ctrlKey){
      event.preventDefault();
    }
  });

  term.attachCustomKeyEventHandler(function (e) {
    if (e.altKey && e.type === "keydown" && e.location === 2) {
        altGrPressed = true;
    } else if (e.altKey && e.type === "keyup" && e.key === "Control") {
      altGrPressed = false
    }

    if (e.type != "keydown" || altGrPressed) {
      return true;
    }

    for (var i = 0; i < window.keyBindings.length; i++) {
      var keyBinding = window.keyBindings[i];
      if (keyBinding.ctrl == e.ctrlKey
        && keyBinding.meta == e.metaKey
        && keyBinding.alt == e.altKey
        && keyBinding.shift == e.shiftKey
        && keyBinding.key == e.keyCode) {
        if (document.visibilityState == 'visible') {
          if (keyBinding.command == 'Copy' && term.getSelection() == '') {
            return true;
          }
          if (keyBinding.command == 'Clear') {
            term.clearSelection();
            term.clear();
            return false;
          }
          if (keyBinding.command == 'SelectAll') {
            term.selectAll();
            return false;
          }

          e.preventDefault();
          window.terminalBridge.invokeCommand(keyBinding.command);
        }
        return false;
      }
    }
    return true;
  });

  window.terminalBridge.initialized();

  return JSON.stringify({
    rows: term.rows,
    columns: term.cols
  });
}

window.changeTheme = (theme) => {
  theme = JSON.parse(theme);
  term.setOption('theme', theme);
  document.body.style.background = theme.background;
}

window.changeOptions = (options) => {
  options = JSON.parse(options);

  term.setOption('bellStyle', options.bellStyle);
  term.setOption('cursorBlink', options.cursorBlink);
  term.setOption('cursorStyle', options.cursorStyle);
  term.setOption('fontFamily', options.fontFamily);
  term.setOption('fontSize', options.fontSize);
  term.setOption('fontWeight', options.fontWeight);
  term.setOption('fontWeightBold', convertBoldText(options.fontWeight));
  term.setOption('scrollback', options.scrollBackLimit);
  term.setOption('wordSeparator', DecodeSpecialChars(options.wordSeparator));
  setScrollBarStyle(options.scrollBarStyle);
  setPadding(options.padding);
}

function setScrollBarStyle(scrollBarStyle) {
  switch (scrollBarStyle) {
    case 'hidden': return terminalContainer.style['-ms-overflow-style'] = 'none';
    case 'autoHiding': return terminalContainer.style['-ms-overflow-style'] = '-ms-autohiding-scrollbar';
    case 'visible': return terminalContainer.style['-ms-overflow-style'] = 'scrollbar';
  }
}

function setPadding(padding) {
  term.element.style.padding = padding + 'px';
  fitAddon.fit();
}

window.changeKeyBindings = (keyBindings) => {
  keyBindings = JSON.parse(keyBindings);
  window["keyBindings"] = keyBindings;
}

window.findNext = (content: string, caseSensitive: boolean, wholeWord: boolean, regex: boolean) => {
  searchAddon.findNext(content, { caseSensitive: caseSensitive, wholeWord: wholeWord, regex: regex });
}

window.findPrevious = (content: string, caseSensitive: boolean, wholeWord: boolean, regex: boolean) => {
  searchAddon.findPrevious(content, { caseSensitive: caseSensitive, wholeWord: wholeWord, regex: regex });
}

document.oncontextmenu = function () {
  return false;
};

function convertBoldText(fontWeight: FontWeight) : FontWeight {
  return parseInt(fontWeight.toString()) > 600 ? '900' : 'bold';
}