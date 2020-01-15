#pragma once
#include "../VM/VM.h"
#include <iostream>
#include <cmath>
#include <chrono>
#include <random>
#include <fcntl.h>

static VM::Value* WriteOutput(VM::Engine* context, size_t argc, VM::Value** argv)
{
	for (size_t i = 0; i < argc; ++i)
	{
		std::wstring s;
		argv[i]->AsString(s);
		std::wcout << s;
	}
	fflush(stdout);
	return context->GC().NewBooleanValue(false);
}

static VM::Value* ReadInput(VM::Engine* context, size_t argc, VM::Value** argv)
{
	for (size_t i = 0; i < argc; ++i)
	{
		std::wstring s;
		argv[i]->AsString(s);
		std::wcout << s;
	}

	std::wstring v;
	std::wcin >> v;
	return context->GC().NewStringValue(v);
}

static VM::Value* ValueToInteger(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc > 0)
		return context->GC().NewIntegerValue(argv[0]->AsInteger());
	return context->GC().NewIntegerValue(0);
}

static VM::Value* ValueToNumber(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc > 0)
		return context->GC().NewRealValue(argv[0]->AsReal());
	return context->GC().NewRealValue(0.0);
}

static VM::Value* ValueToString(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc > 0)
	{
		std::wstring s;
		argv[0]->AsString(s);
		return context->GC().NewStringValue(s);
	}
	return context->GC().NewStringValue(nullptr, 0);
}

static VM::Value* ValueFloor(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc > 0)
		return context->GC().NewIntegerValue(static_cast<int64_t>(std::floor(argv[0]->AsReal())));
	return context->GC().NewIntegerValue(0);
}

static VM::Value* ValueCeiling(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc > 0)
		return context->GC().NewIntegerValue(static_cast<int64_t>(std::ceil(argv[0]->AsReal())));
	return context->GC().NewIntegerValue(0);
}

static VM::Value* GetArrayRow(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc > 0)
	{
		return context->GC().NewIntegerValue(static_cast<uint64_t>(argv[0]->GetRow()));
	}
	return context->GC().NewIntegerValue(0);
}

static VM::Value* GetArrayCol(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc > 0)
	{
		return context->GC().NewIntegerValue(static_cast<uint64_t>(argv[0]->GetCol()));
	}
	return context->GC().NewIntegerValue(0);
}

static VM::Value* XSetConsoleTitle(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc > 0)
		return argv[0];
	return context->GC().NewStringValue(L"", -1);
}

static VM::Value* SetConsoleBackgroundColor(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc > 0)
	{
		std::wstring colorCode;
		auto color = argv[0]->AsString();
		if (color == L"Black")
			colorCode = L"\033[40m";
		else if (color == L"DarkBlue")
			colorCode = L"\033[44m";
		else if (color == L"DarkGreen")
			colorCode = L"\033[46m";
		else if (color == L"DarkCyan")
			colorCode = L"\033[45m";
		else if (color == L"DarkRed")
			colorCode = L"\033[41m";
		else if (color == L"DarkMagenta")
			colorCode = L"\033[45m";
		else if (color == L"DarkYellow")
			colorCode = L"\033[43m";
		else if (color == L"DarkWhite")
			colorCode = L"\033[47m";
		else if (color == L"Blue")
			colorCode = L"\033[1;44m";
		else if (color == L"Green")
			colorCode = L"\033[1;42m";
		else if (color == L"Cyan")
			colorCode = L"\033[1;45m";
		else if (color == L"Red")
			colorCode = L"\033[1;41m";
		else if (color == L"Magenta")
			colorCode = L"\033[1;45m";
		else if (color == L"Yellow")
			colorCode = L"\033[1;43m";
		else if (color == L"White")
			colorCode = L"\033[47m";

		std::wcout << colorCode;
		return context->GC().NewStringValue(color);
	}
	return context->GC().NewStringValue(nullptr, 0);
}

static VM::Value* SetConsoleForegroundColor(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc > 0)
	{
		std::wstring colorCode;
		auto color = argv[0]->AsString();
		if (color == L"Black")
			colorCode = L"\033[30m";
		else if (color == L"DarkBlue")
			colorCode = L"\033[34m";
		else if (color == L"DarkGreen")
			colorCode = L"\033[36m";
		else if (color == L"DarkCyan")
			colorCode = L"\033[35m";
		else if (color == L"DarkRed")
			colorCode = L"\033[31m";
		else if (color == L"DarkMagenta")
			colorCode = L"\033[35m";
		else if (color == L"DarkYellow")
			colorCode = L"\033[33m";
		else if (color == L"DarkWhite")
			colorCode = L"\033[37m";
		else if (color == L"Blue")
			colorCode = L"\033[1;34m";
		else if (color == L"Green")
			colorCode = L"\033[1;32m";
		else if (color == L"Cyan")
			colorCode = L"\033[1;35m";
		else if (color == L"Red")
			colorCode = L"\033[1;31m";
		else if (color == L"Magenta")
			colorCode = L"\033[1;35m";
		else if (color == L"Yellow")
			colorCode = L"\033[1;33m";
		else if (color == L"White")
			colorCode = L"\033[37m";

		std::wcout << colorCode;
		return context->GC().NewStringValue(color);
	}
	return context->GC().NewStringValue(nullptr, 0);
}

static VM::Value* XSetConsoleCursorPosition(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc == 2)
	{
		std::wstring code;
		code = L"\033[";
		auto x = (uint16_t)argv[0]->AsInteger();
		auto y = (uint16_t)argv[1]->AsInteger();
		code += std::to_wstring(y);
		code += L";";
		code += std::to_wstring(x);
		code += L"H";

		std::wcout << code;
	}
	return context->GC().NewBooleanValue(true);
}

static VM::Value* GetNewLine(VM::Engine* context, size_t argc, VM::Value** argv)
{
	return context->GC().NewStringValue(L"\r\n");
}

static const wchar_t* ConsoleKeyNames[] =
{
	L"None",//0
	L"None",
	L"None",
	L"None",
	L"None",
	L"None",
	L"None",
	L"None",
	L"Backspace",
	L"Tab",
	L"None",//10
	L"None",
	L"Clear",
	L"Enter",
	L"None",
	L"None",
	L"None",
	L"None",
	L"None",
	L"Pause",
	L"None",//20
	L"None",
	L"None",
	L"None",
	L"None",
	L"None",
	L"None",
	L"Escape",
	L"None",
	L"None",
	L"None",//30
	L"None",
	L"Spacebar",
	L"PageUp",
	L"PageDown",
	L"End",
	L"Home",
	L"LeftArrow",
	L"UpArrow",
	L"RightArrow",
	L"DownArrow",//40
	L"Select",
	L"Print",
	L"Execute",
	L"PrintScreen",
	L"Insert",
	L"Delete",
	L"Help",
	L"D0",
	L"D1",
	L"D2",//50
	L"D3",
	L"D4",
	L"D5",
	L"D6",
	L"D7",
	L"D8",
	L"D9",
	L"None",
	L"None",
	L"None",//60
	L"None",
	L"None",
	L"None",
	L"None",
	L"A",//65
	L"B",
	L"C",
	L"D",
	L"E",
	L"F",//70
	L"G",
	L"H",
	L"I",
	L"J",
	L"K",
	L"L",
	L"M",
	L"N",
	L"O",
	L"P",//80
	L"Q",
	L"R",
	L"S",
	L"T",
	L"U",
	L"V",
	L"W",
	L"X",
	L"Y",
	L"Z",//90
	L"LeftWindows",
	L"RightWindows",
	L"Applications",
	L"None",
	L"Sleep",
	L"NumPad0",
	L"NumPad1",
	L"NumPad2",
	L"NumPad3",
	L"NumPad4",//100
	L"NumPad5",
	L"NumPad6",
	L"NumPad7",
	L"NumPad8",
	L"NumPad9",
	L"Multiply",
	L"Add",
	L"Separator",
	L"Subtract",
	L"Decimal",//110
	L"Divide",
	L"F1",
	L"F2",
	L"F3",
	L"F4",
	L"F5",
	L"F6",
	L"F7",
	L"F8",
	L"F9",//120
	L"F10",
	L"F11",
	L"F12",
	L"F13",
	L"F14",
	L"F15",
	L"F16",
	L"F17",
	L"F18",
	L"F19",//130
	L"F20",
	L"F21",
	L"F22",
	L"F23",
	L"F24"
};

static const wchar_t* scanKeyboard()
{
	int in = 0;
	int ch = 0;
	struct termios oldt, newt;
	int oldf;
	tcgetattr(STDIN_FILENO, &oldt);
	newt = oldt;
	newt.c_lflag &= ~(ICANON | ECHO);
	tcsetattr(STDIN_FILENO, TCSANOW, &newt);
	oldf = fcntl(STDIN_FILENO, F_GETFL, 0);
	fcntl(STDIN_FILENO, F_SETFL, oldf | O_NONBLOCK);
	;
	while (read(STDIN_FILENO, &ch, 1)>0)
	{
		in = ch;
	}
	tcsetattr(STDIN_FILENO, TCSANOW, &oldt);
	fcntl(STDIN_FILENO, F_SETFL, oldf);
	
	if (in >= 'a' && in <= 'z')
	{
		in = in - 32;
	}
	return ConsoleKeyNames[in];
}


static VM::Value* ReadInputKey(VM::Engine* context, size_t argc, VM::Value** argv)
{
	std::wstring key = scanKeyboard();
	return context->GC().NewStringValue(key);
}

static VM::Value* ReadGVar(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc >= 1)
		return context->GetGlobalVariable(argv[0]->AsString());
	return context->GC().NewBooleanValue(false);
}

static VM::Value* WriteGVar(VM::Engine* context, size_t argc, VM::Value** argv)
{
	if (argc >= 2)
	{
		context->SetGlobalVariable(argv[0]->AsString(), argv[1]);
		return argv[1];
	}
	return context->GC().NewBooleanValue(false);
}

static VM::Value* ReadTimeMS(VM::Engine* context, size_t argc, VM::Value** argv)
{
	auto t = std::chrono::system_clock::now().time_since_epoch();
	auto mil = std::chrono::duration_cast<std::chrono::milliseconds>(t);
	return context->GC().NewIntegerValue(mil.count());
}

static VM::Value* GetRandom(VM::Engine* context, size_t argc, VM::Value** argv)
{
	static std::default_random_engine e(
		static_cast<unsigned int>
		(std::chrono::duration_cast<std::chrono::microseconds>(
			std::chrono::system_clock::now().time_since_epoch()
			)
			.count())
	);

	if (argc == 0)
	{
		std::uniform_real_distribution<double> u(0, 1);
		return context->GC().NewRealValue(u(e));
	}
	else if (argc == 1)
	{
		if (argv[0]->Is(VM::Value::Integer))
		{
			std::uniform_int_distribution<int64_t> u(0, argv[0]->AsInteger());
			auto n = u(e);
			return context->GC().NewIntegerValue(n);
		}
		else
		{
			std::uniform_real_distribution<double> u(0, argv[0]->AsReal());
			return context->GC().NewRealValue(u(e));
		}
	}
	else if (argc >= 2)
	{
		if (argv[0]->Is(VM::Value::Integer))
		{
			std::uniform_int_distribution<int64_t> u(argv[0]->AsInteger(), argv[1]->AsInteger());
			return context->GC().NewIntegerValue(u(e));
		}
		else
		{
			std::uniform_real_distribution<double> u(argv[0]->AsReal(), argv[1]->AsReal());
			return context->GC().NewRealValue(u(e));
		}
	}
	else
	{
		std::uniform_real_distribution<double> u(0, 1);
		return context->GC().NewRealValue(u(e));
	}
}