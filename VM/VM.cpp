#include "VM.h"
#include <locale>
#include <codecvt>
#include <cassert>
#include <cstring>

namespace VM
{
	Engine::Engine() :
		mConstants(),
		mInstructionCount(0),
		mInstructions(nullptr),
		mIP(nullptr),
		mCallParameters(),
		mCallStack(),
		mCALCStack(),
		mDATAStack(nullptr),
		mGlobalVariableTable(),
		mGC()
	{
		mCallParameters.reserve(1024);
	}

	Engine::~Engine()
	{
		ClearProgram();
	}

	size_t Engine::AppendHostCall(PFN_HOST_CALL hostCall)
	{
		auto r = mHostCalls.size();
		mHostCalls.push_back(hostCall);
		return r;
	}

	void Engine::LoadProgram(std::fstream& in)
	{
		const uint8_t file_flag[] = { 0xDA,0xE6,0x9F,0xF3,0xF6,0x98,0x54,0x48,0xB0,0xCB,0x65,0x9E,0xF6,0xB8,0x38,0xCE };
		ClearProgram();
		uint8_t flagbuffer[16];
		ReadBytes(in, flagbuffer, sizeof(flagbuffer));
		if (memcmp(flagbuffer, file_flag, sizeof(flagbuffer)) != 0)
			throw Exception(10001, "File is not in the correct format.");
		uint32_t constantsCount = ReadNumber<uint32_t>(in);
		uint32_t instructionCount = ReadNumber<uint32_t>(in);
		ReadNumber<uint64_t>(in);
		in.seekg(24, std::ios::cur);

		for (uint32_t i = 0; i < constantsCount; ++i)
		{
			mConstants.push_back(ReadValue(in));
		}

		mInstructions = new Instruction[instructionCount];
		Instruction* instruction = mInstructions;
		for (uint32_t i = 0; i < instructionCount; ++i)
		{
			ReadInstruction(in, instruction++);
		}
		mInstructionCount = instructionCount;
	}

	InstructionID Engine::ReadIID(std::fstream& in)
	{
		auto id = ReadNumber<uint16_t>(in);
		return static_cast<InstructionID>(id);
	}

	template<class TNumber>
	TNumber Engine::ReadNumber(std::fstream& in)
	{
		TNumber value;
		in.read(reinterpret_cast<char*>(&value), sizeof(value));
		return value;
	}

	uint64_t Engine::Read7BitInt(std::fstream& in)
	{
		uint64_t mask = 0x7F;
		uint64_t temp = 0;
		uint64_t result = 0;
		size_t bit = 0;

		for (size_t i = 0; (!in.eof()) && i < 10; ++i)
		{
			uint8_t ch = 0;
			in.read(reinterpret_cast<char*>(&ch), sizeof(ch));
			temp = ch;
			result = result | ((temp & mask) << bit);
			if ((temp & 0x80) != 0x80)
				break;
			bit += 7;
		}
		return result;
	}

	void Engine::ReadString(std::fstream& in, std::wstring& value)
	{
		std::wstring_convert<std::codecvt_utf8<wchar_t>> strCnv;
		auto len = static_cast<size_t>(Read7BitInt(in));
		std::vector<char> utf8;
		utf8.resize(len);
		in.read(utf8.data(), len);
		value = strCnv.from_bytes(utf8.data(), utf8.data() + utf8.size());
	}

	bool Engine::ReadBoolean(std::fstream& in)
	{
		uint8_t bytes[2];
		ReadBytes(in, bytes, sizeof(bytes));
		return bytes[0] == 0x00 && bytes[1] == 0xFF;
	}

	void Engine::ReadBytes(std::fstream& in, void* buffer, size_t size)
	{
		in.read(reinterpret_cast<char*>(buffer), size);
	}
	void Engine::ReadInstruction(std::fstream& in, Instruction* instruction)
	{
		auto iid = ReadIID(in);
		switch (iid)
		{
		case InstructionID::NOOP:
			instruction->func = &Engine::InstructionNOOP;
			instruction->tag = 0;
			break;
		case InstructionID::ADD:
			instruction->func = &Engine::InstructionADD;
			instruction->tag = 0;
			break;
		case InstructionID::AND:
			instruction->func = &Engine::InstructionAND;
			instruction->tag = 0;
			break;
		case InstructionID::ALLOCDSTK:
			instruction->func = &Engine::InstructionALLOCDSTK;
			instruction->tag = static_cast<size_t>(Read7BitInt(in));
			break;
		case InstructionID::ARRAYMAKE:
			instruction->func = &Engine::InstructionARRAYMAKE;
			instruction->tag = 0;
			break;
		case InstructionID::ARRAYREAD:
			instruction->func = &Engine::InstructionARRAYREAD;
			instruction->tag = static_cast<size_t>(Read7BitInt(in));
			break;
		case InstructionID::ARRAYWRITE:
			instruction->func = &Engine::InstructionARRAYWRITE;
			instruction->tag = static_cast<size_t>(Read7BitInt(in));
			break;
		case InstructionID::CALL:
			instruction->func = &Engine::InstructionCALL;
			instruction->tag = static_cast<size_t>(Read7BitInt(in));
			break;
		case InstructionID::CALLSYS:
			instruction->func = &Engine::InstructionCALLSYS;
			instruction->tag = static_cast<size_t>(Read7BitInt(in));
			break;
		case InstructionID::DIV:
			instruction->func = &Engine::InstructionDIV;
			instruction->tag = 0;
			break;
		case InstructionID::EQ:
			instruction->func = &Engine::InstructionEQ;
			instruction->tag = 0;
			break;
		case InstructionID::GT:
			instruction->func = &Engine::InstructionGT;
			instruction->tag = 0;
			break;
		case InstructionID::JMP:
			instruction->func = &Engine::InstructionJMP;
			instruction->tag = static_cast<size_t>(Read7BitInt(in));
			break;
		case InstructionID::JMPC:
			instruction->func = &Engine::InstructionJMPC;
			instruction->tag = static_cast<size_t>(Read7BitInt(in));
			break;
		case InstructionID::JMPN:
			instruction->func = &Engine::InstructionJMPN;
			instruction->tag = static_cast<size_t>(Read7BitInt(in));
			break;
		case InstructionID::LT:
			instruction->func = &Engine::InstructionLT;
			instruction->tag = 0;
			break;
		case InstructionID::LC:
			instruction->func = &Engine::InstructionLC;
			instruction->tag = static_cast<size_t>(Read7BitInt(in));
			break;
		case InstructionID::LD:
			instruction->func = &Engine::InstructionLD;
			instruction->tag = static_cast<size_t>(Read7BitInt(in));
			break;
		case InstructionID::MOD:
			instruction->func = &Engine::InstructionMOD;
			instruction->tag = 0;
			break;
		case InstructionID::MUL:
			instruction->func = &Engine::InstructionMUL;
			instruction->tag = 0;
			break;
		case InstructionID::NE:
			instruction->func = &Engine::InstructionNE;
			instruction->tag = 0;
			break;
		case InstructionID::NOT:
			instruction->func = &Engine::InstructionNOT;
			instruction->tag = 0;
			break;
		case InstructionID::OR:
			instruction->func = &Engine::InstructionOR;
			instruction->tag = 0;
			break;
		case InstructionID::POP:
			instruction->func = &Engine::InstructionPOP;
			instruction->tag = 0;
			break;
		case InstructionID::PUSH:
			instruction->func = &Engine::InstructionPUSH;
			instruction->tag = 0;
			break;
		case InstructionID::RET:
			instruction->func = &Engine::InstructionRET;
			instruction->tag = 0;
			break;
		case InstructionID::SUB:
			instruction->func = &Engine::InstructionSUB;
			instruction->tag = 0;
			break;
		case InstructionID::SD:
			instruction->func = &Engine::InstructionSD;
			instruction->tag = static_cast<size_t>(Read7BitInt(in));
			break;
		default:
			throw Exception(10003, "Unrecognized instruction.");
		}
	}

	Value* Engine::ReadValue(std::fstream& in)
	{
		Value* result = nullptr;
		uint8_t type[2];
		ReadBytes(in, type, sizeof(type));
		if (type[1] == 0)
		{
			switch (static_cast<Value::Type>(type[0]))
			{
			case Value::Integer:
				result = mGC.RawMemory().NewValue(static_cast<int64_t>(Read7BitInt(in)));
				break;
			case Value::Real:
				result = mGC.RawMemory().NewValue(ReadNumber<double>(in));
				break;
			case Value::String:
			{
				std::wstring s;
				ReadString(in, s);
				result = mGC.RawMemory().NewValue(s);
			}
			break;
			case Value::Boolean:
				result = mGC.RawMemory().BooleanValue(ReadBoolean(in));
				break;
			case Value::Array:
			{
				size_t rx = static_cast<size_t>(Read7BitInt(in));
				size_t cx = static_cast<size_t>(Read7BitInt(in));
				auto arr = mGC.RawMemory().NewValue(rx, cx, nullptr);
				try
				{
					for (size_t r = 0; r < rx; ++r)
					{
						for (size_t c = 0; c < cx; ++c)
						{
							arr->SetValue(r, c, ReadValue(in));
						}
					}
					result = arr;
				}
				catch (const Exception&)
				{
					mGC.RawMemory().FreeValue(arr);
					throw;
				}
			}
			break;
			default:
				throw Exception(10002, "Data type is not supported.");
				break;
			}
		}
		else
		{
			throw Exception(10002, "Data type is not supported.");
		}
		return result;
	}

	void Engine::ClearProgram(void)
	{
		for (auto v : mConstants)
		{
			mGC.RawMemory().FreeValue(v);
		}
		mConstants.clear();

		if (mInstructions != nullptr)
			delete[] mInstructions;
		mInstructions = nullptr;
		mInstructionCount = 0;

		mIP = 0;

		for (auto n : mCallStack)
		{
			n.datastack->clear();
			delete n.datastack;
		}
		mCallStack.clear();

		mCALCStack.clear();

		mDATAStack = nullptr;

		mCallParameters.clear();

		mGlobalVariableTable.clear();

		mGC.Clean();
	}

	int Engine::Run(void)
	{
		mIP = mInstructions;
		const Instruction* end = mInstructions + mInstructionCount;
		CallNode cn =
		{
			end,
			nullptr
		};
		mCallStack.push_back(cn);
		mGC.Start();
		while (mIP < end)
		{
			(this->*(mIP->func))(mIP->tag);
			++mIP;
			mGC.CheckMemoryGC(this);
		}
		return static_cast<int>(CALCStackPop()->AsReal());
	}


	void Engine::SetGlobalVariable(const std::wstring& name, Value* value)
	{
		mGlobalVariableTable[name] = value;
	}

	Value* Engine::GetGlobalVariable(const std::wstring& name)
	{
		auto it = mGlobalVariableTable.find(name);
		if (it == mGlobalVariableTable.end())
			return mGC.NewBooleanValue(false);
		return it->second;
	}

	void Engine::DATAStackAlloc(size_t size)
	{
		mDATAStack = new std::vector<Value*>();
		mDATAStack->reserve(size);
		for (size_t i = 0; i < size; ++i)
		{
			mDATAStack->push_back(mGC.NewBooleanValue(false));
		}
	}

	Value* Engine::DATAStackGet(size_t index)
	{
		return mDATAStack->at(index);
	}

	void Engine::DATAStackPut(size_t index, Value* value)
	{
		mDATAStack->at(index) = value;
	}

	Value* Engine::CALCStackPop(void)
	{
		auto r = mCALCStack.back();
		mCALCStack.pop_back();
		return r;
	}

	void Engine::CALCStackPush(Value* value)
	{
		mCALCStack.push_back(value);
	}

	void Engine::InstructionAND(size_t tag)
	{
		auto b = CALCStackPop();
		auto a = CALCStackPop();
		auto r = mGC.NewBooleanValue(a->AsBoolean() && b->AsBoolean());
		CALCStackPush(r);
	}
	void Engine::InstructionADD(size_t tag)
	{
		auto b = CALCStackPop();
		auto a = CALCStackPop();
		Value* r;
		if (a->GetType() == b->GetType())
		{
			switch (a->GetType())
			{
			case Value::String:
			{
				std::wstring sa;
				std::wstring sb;
				a->AsString(sa);
				b->AsString(sb);
				r = mGC.NewStringValue(sa.append(sb));
			}
			break;
			case Value::Integer:
			{
				r = mGC.NewIntegerValue(a->AsInteger() + b->AsInteger());
			}
			break;
			default:
			{
				r = mGC.NewRealValue(a->AsReal() + b->AsReal());
			}
			break;
			}
		}
		else
		{
			if (a->Is(Value::String) || b->Is(Value::String))
			{
				std::wstring sa;
				std::wstring sb;
				a->AsString(sa);
				b->AsString(sb);
				r = mGC.NewStringValue(sa.append(sb));
			}
			else
			{
				r = mGC.NewRealValue(a->AsReal() + b->AsReal());
			}
		}

		CALCStackPush(r);
	}
	void Engine::InstructionALLOCDSTK(size_t tag)
	{
		DATAStackAlloc(tag);
	}
	void Engine::InstructionARRAYMAKE(size_t tag)
	{
		auto vf = CALCStackPop();
		auto vc = CALCStackPop();
		auto vr = CALCStackPop();

		auto r = mGC.NewArrayValue(
			static_cast<size_t>(vr->AsReal()),
			static_cast<size_t>(vc->AsReal()),
			vf);
		CALCStackPush(r);
	}
	void Engine::InstructionARRAYREAD(size_t tag)
	{
		auto vc = CALCStackPop();
		auto vr = CALCStackPop();
		auto d = DATAStackGet(tag);
		auto r = d->GetValue(
			static_cast<size_t>(vr->AsReal()),
			static_cast<size_t>(vc->AsReal())
		);
		CALCStackPush(r);
	}
	void Engine::InstructionARRAYWRITE(size_t tag)
	{
		auto vv = CALCStackPop();
		auto vc = CALCStackPop();
		auto vr = CALCStackPop();

		auto d = DATAStackGet(tag);
		d->SetValue(
			static_cast<size_t>(vr->AsReal()),
			static_cast<size_t>(vc->AsReal()),
			vv
		);
	}
	void Engine::InstructionCALL(size_t tag)
	{
		CallNode cn =
		{
			mIP,
			mDATAStack
		};
		mCallStack.push_back(cn);
		mIP = mInstructions + (tag - 1);
	}
	void Engine::InstructionCALLSYS(size_t tag)
	{
		auto pc = (tag & 0xFFC00000) >> 22;
		auto idx = tag & 0x003FFFFF;
		if (idx >= mHostCalls.size())
			throw Exception(20001, "Function index out of bounds.");

		auto sc = mHostCalls[idx];
		mCallParameters.clear();
		for (size_t i = 0; i < pc; ++i)
			mCallParameters.push_back(CALCStackPop());
		auto r = sc(this, pc, mCallParameters.data());
		mCallParameters.clear();
		CALCStackPush(r);
	}
	void Engine::InstructionDIV(size_t tag)
	{
		auto b = CALCStackPop();
		auto a = CALCStackPop();
		Value* r;
		if (a->GetType() == b->GetType() && a->GetType() == Value::Integer)
		{
			r = mGC.NewIntegerValue(a->AsInteger() / b->AsInteger());
		}
		else
		{
			r = mGC.NewRealValue(a->AsReal() / b->AsReal());
		}
		CALCStackPush(r);
	}
	void Engine::InstructionEQ(size_t tag)
	{
		auto b = CALCStackPop();
		auto a = CALCStackPop();
		auto r = mGC.NewBooleanValue(a->VEquals(b));
		CALCStackPush(r);
	}
	void Engine::InstructionGT(size_t tag)
	{
		auto b = CALCStackPop();
		auto a = CALCStackPop();
		Value* r;
		if (a->Is(Value::String) || b->Is(Value::String))
		{
			std::wstring sa;
			std::wstring sb;
			a->AsString(sa);
			b->AsString(sb);
			r = mGC.NewBooleanValue(sa.size() > sb.size());
		}
		else
		{
			r = mGC.NewBooleanValue(a->AsReal() > b->AsReal());
		}
		CALCStackPush(r);
	}
	void Engine::InstructionJMP(size_t tag)
	{
		mIP = mInstructions + (tag - 1);
	}
	void Engine::InstructionJMPC(size_t tag)
	{
		auto a = CALCStackPop();
		if (a->AsBoolean())
			mIP = mInstructions + (tag - 1);
	}
	void Engine::InstructionJMPN(size_t tag)
	{
		auto a = CALCStackPop();
		if (!a->AsBoolean())
			mIP = mInstructions + (tag - 1);
	}
	void Engine::InstructionLT(size_t tag)
	{
		auto b = CALCStackPop();
		auto a = CALCStackPop();
		Value* r;
		if (a->Is(Value::String) || b->Is(Value::String))
		{
			std::wstring sa;
			std::wstring sb;
			a->AsString(sa);
			b->AsString(sb);
			r = mGC.NewBooleanValue(sa.size() < sb.size());
		}
		else
		{
			r = mGC.NewBooleanValue(a->AsReal() < b->AsReal());
		}
		CALCStackPush(r);
	}
	void Engine::InstructionLC(size_t tag)
	{
		auto r = mConstants[tag];
		CALCStackPush(r);
	}
	void Engine::InstructionLD(size_t tag)
	{
		auto r = DATAStackGet(tag);
		CALCStackPush(r);
	}
	void Engine::InstructionMOD(size_t tag)
	{
		auto b = CALCStackPop();
		auto a = CALCStackPop();
		Value* r = mGC.NewIntegerValue(a->AsInteger() % b->AsInteger());
		CALCStackPush(r);
	}
	void Engine::InstructionMUL(size_t tag)
	{
		auto b = CALCStackPop();
		auto a = CALCStackPop();
		Value* r;
		if (a->GetType() == b->GetType() && a->GetType() == Value::Integer)
		{
			r = mGC.NewIntegerValue(a->AsInteger() * b->AsInteger());
		}
		else
		{
			r = mGC.NewRealValue(a->AsReal() * b->AsReal());
		}
		CALCStackPush(r);
	}
	void Engine::InstructionNE(size_t tag)
	{
		auto b = CALCStackPop();
		auto a = CALCStackPop();
		auto r = mGC.NewBooleanValue(!a->VEquals(b));
		CALCStackPush(r);
	}
	void Engine::InstructionNOT(size_t tag)
	{
		auto a = CALCStackPop();
		auto r = mGC.NewBooleanValue(!a->AsBoolean());
		CALCStackPush(r);
	}
	void Engine::InstructionOR(size_t tag)
	{
		auto b = CALCStackPop();
		auto a = CALCStackPop();
		auto r = mGC.NewBooleanValue(
			a->AsBoolean() ||
			b->AsBoolean());
		CALCStackPush(r);
	}
	void Engine::InstructionPOP(size_t tag)
	{
		CALCStackPop();
	}
	void Engine::InstructionPUSH(size_t tag)
	{
		CALCStackPush(mGC.NewBooleanValue(false));
	}
	void Engine::InstructionRET(size_t tag)
	{
		const CallNode& cn = mCallStack.back();
		delete mDATAStack;
		mIP = cn.ip;
		mDATAStack = cn.datastack;
		mCallStack.pop_back();
	}

	static std::wstring& trim_end(std::wstring &s)
	{
		if (s.empty())
			return s;

		s.erase(s.find_last_not_of(L" ") + 1);
		return s;
	}

	static std::wstring& trim_start(std::wstring &s)
	{
		if (s.empty())
			return s;

		s.erase(0, s.find_first_not_of(L" "));
		return s;
	}

	void Engine::InstructionSUB(size_t tag)
	{
		auto b = CALCStackPop();
		auto a = CALCStackPop();
		Value* r;
		if (a->GetType() == b->GetType())
		{
			switch (a->GetType())
			{
			case Value::String:
			{
				std::wstring sa;
				std::wstring sb;
				a->AsString(sa);
				b->AsString(sb);
				r = mGC.NewStringValue(sa.append(sb));
			}
			break;
			case Value::Integer:
			{
				r = mGC.NewIntegerValue(a->AsInteger() - b->AsInteger());
			}
			break;
			default:
			{
				r = mGC.NewRealValue(a->AsReal() - b->AsReal());
			}
			break;
			}
		}
		else
		{
			if (a->Is(Value::String) || b->Is(Value::String))
			{
				std::wstring sa;
				std::wstring sb;
				a->AsString(sa);
				b->AsString(sb);

				r = mGC.NewStringValue(trim_end(sa).append(trim_start(sb)));
			}
			else
			{
				r = mGC.NewRealValue(a->AsReal() - b->AsReal());
			}
		}

		CALCStackPush(r);
	}
	void Engine::InstructionSD(size_t tag)
	{
		auto v = CALCStackPop();
		DATAStackPut(tag, v);
	}









	bool Value::VEquals(const Value* value)const
	{
		if (value == this)
			return true;
		if (value->Is(GetType()))
		{
			switch (GetType())
			{
			case Integer: return mValue.iValue == value->mValue.iValue;
			case Real: return mValue.dValue == value->mValue.dValue;
			case String:
			{
				if (mValue.sValue.length == value->mValue.sValue.length)
				{
					return memcmp(
						mValue.sValue.str,
						value->mValue.sValue.str,
						value->mValue.sValue.length * sizeof(wchar_t))==0;
				}
				return false;
			}
			case Boolean: return mValue.bValue == value->mValue.bValue;
			case Array:
				return false;
			default:
				break;
			}
		}
		else
		{
			if (Is(String) || value->Is(String))
			{
				std::wstring s0;
				std::wstring s1;
				AsString(s0);
				value->AsString(s1);
				return s0 == s1;
			}
			else if (Is(Real))
			{
				return AsReal() == value->AsReal();
			}
			else if (Is(Boolean))
			{
				return AsBoolean() == value->AsBoolean();
			}
		}
		return false;
	}

	bool Value::AsBoolean(void) const
	{
		switch (GetType())
		{
		case Integer: return mValue.iValue != int64_t(0);
		case Real: return mValue.dValue != double(0);
		case String: return (mValue.sValue.length != 0);
		case Boolean: return mValue.bValue;
		case Array: return true;
		default:
			break;
		}
		return false;
	}

	void Value::AsString(std::wstring& s) const
	{
		switch (GetType())
		{
		case Integer: s = std::to_wstring(mValue.iValue); break;
		case Real:
		{
			s = std::to_wstring(mValue.dValue);
			s.erase(s.find_last_not_of(L'0') + 1, std::string::npos);
			s.erase(s.find_last_not_of(L'.') + 1, std::string::npos);
		}
		break;
		case String: s.assign(mValue.sValue.str, mValue.sValue.length); break;
		case Boolean: s = mValue.bValue ? L"True" : L"False"; break;
		case Array: s = L"[" + std::to_wstring(mValue.aValue.row) + L"," + std::to_wstring(mValue.aValue.col) + L"]"; break;
		default:
			s = std::wstring();
			break;
		}
	}

	int64_t Value::AsInteger(void) const
	{
		switch (GetType())
		{
		case Integer: return mValue.iValue;
		case Real: return static_cast<int64_t>(mValue.dValue);
		case String: return std::wcstoll(mValue.sValue.str, nullptr, 10);
		case Boolean: return mValue.bValue ? int64_t(1) : int64_t(0);
		case Array: return int64_t(0);
		default:
			break;
		}
		return int64_t(0);
	}

	double Value::AsReal(void) const
	{
		switch (GetType())
		{
		case Integer: return static_cast<double>(mValue.iValue);
		case Real: return mValue.dValue;
		case String: return std::wcstod(mValue.sValue.str, nullptr);
		case Boolean: return mValue.bValue ? double(1) : double(0);
		case Array: return double(0);
		default:
			break;
		}
		return double(0);
	}

	size_t Value::GetRow(void)const
	{
		if (Is(Array))
			return mValue.aValue.row;
		return 0;
	}
	size_t Value::GetCol(void)const
	{
		if (Is(Array))
			return mValue.aValue.col;
		return 0;
	}
	Value* Value::GetValue(size_t r, size_t c) const
	{
		if (Is(Array))
		{
			if (r < mValue.aValue.row && c < mValue.aValue.col)
			{
				auto result = mValue.aValue.data[r*mValue.aValue.col + c];
				if (result != nullptr)
					return result;
			}
		}
		return MemoryAllocator::BooleanValue(false);
	}
	void Value::SetValue(size_t r, size_t c, Value* v)
	{
		if (Is(Array))
		{
			if (r < mValue.aValue.row && c < mValue.aValue.col)
			{
				if (IsGCMarked())
					v->GCMarkSet();
				mValue.aValue.data[r*mValue.aValue.col + c] = v;
			}
		}
	}




	MemoryGC::MemoryGC(void) :
		mMemoryPool(),
		mGenerationFullFlags{ false,false,false,false },
		mGeneration()
	{

	}
	MemoryGC::~MemoryGC(void)
	{
		Clean();
	}


	void MemoryGC::GCMarkRoots(Engine* engine)
	{
		for (auto v : engine->mCallParameters)
		{
			v->GCMarkSet();
		}

		for (auto& n : engine->mCallStack)
		{
			if (n.datastack != nullptr)
			{
				for (auto v : *(n.datastack))
				{
					v->GCMarkSet();
				}
			}
		}

		for (auto v : engine->mCALCStack)
		{
			v->GCMarkSet();
		}

		if (engine->mDATAStack != nullptr)
		{
			for (auto v : *(engine->mDATAStack))
			{
				v->GCMarkSet();
			}
		}

		for (auto v : engine->mGlobalVariableTable)
		{
			v.second->GCMarkSet();
		}
	}

	void MemoryGC::GCGenerationMarkClear(int gen)
	{
		auto g = mGeneration + gen;

		for (auto v : *g)
		{
			v->GCMarkClear();
		}
	}

	void MemoryGC::GCGenerationClean(int gen)
	{
		if (gen >= 3)
		{
			mGenerationFullFlags[gen] = false;
			auto generation = mGeneration + gen;
			auto i = generation->begin();
			while (i != generation->end())
			{
				if ((*i)->IsGCMarkFreed())
				{
					RawMemory().FreeValue(*i);
					i = generation->erase(i);
				}
				else
				{
					++i;
				}
			}
		}
		else
		{
			mGenerationFullFlags[gen] = false;
			auto generation = mGeneration + gen;
			auto nextGeneration = mGeneration + (++gen);
			while (generation->size() > 0)
			{
				auto v = generation->back();
				if (v->IsGCMarkFreed())
				{
					RawMemory().FreeValue(v);
				}
				else
				{
					nextGeneration->push_back(v);
				}
				generation->pop_back();
			}

			if (nextGeneration->size() > nextGeneration->capacity() - 32)
			{
				mGenerationFullFlags[gen] = true;
			}
		}
	}

	void MemoryGC::GC(Engine* engine)
	{
		if (mGenerationFullFlags[3])
		{
			GCGenerationMarkClear(3);
			GCMarkRoots(engine);
			GCGenerationClean(3);
		}
		if (mGenerationFullFlags[2])
		{
			GCGenerationMarkClear(2);
			GCMarkRoots(engine);
			GCGenerationClean(2);
		}
		if (mGenerationFullFlags[1])
		{
			GCGenerationMarkClear(1);
			GCMarkRoots(engine);
			GCGenerationClean(1);
		}

		GCGenerationMarkClear(0);
		GCMarkRoots(engine);
		GCGenerationClean(0);
	}

	void MemoryGC::CheckMemoryGC(Engine* engine)
	{
		if (mGeneration->size() > mGeneration->capacity() - 32)
		{
			GC(engine);
		}
	}

	Value* MemoryGC::NewIntegerValue(int32_t value)
	{
		return NewIntegerValue(static_cast<int64_t>(value));
	}
	Value* MemoryGC::NewIntegerValue(uint32_t value)
	{
		return NewIntegerValue(static_cast<int64_t>(value));
	}

	Value* MemoryGC::NewIntegerValue(int64_t value)
	{
		auto v = RawMemory().NewValue(value);
		mGeneration->push_back(v);
		return v;
	}
	Value* MemoryGC::NewIntegerValue(uint64_t value)
	{
		return NewIntegerValue(static_cast<int64_t>(value));
	}
	Value* MemoryGC::NewRealValue(float value)
	{
		return NewRealValue(static_cast<double>(value));
	}
	Value* MemoryGC::NewRealValue(double value)
	{
		auto v = RawMemory().NewValue(value);
		mGeneration->push_back(v);
		return v;
	}
	Value* MemoryGC::NewStringValue(const std::wstring& value)
	{
		auto v = RawMemory().NewValue(value);
		mGeneration->push_back(v);
		return v;
	}
	Value* MemoryGC::NewStringValue(const wchar_t* value, size_t length)
	{
		auto v = RawMemory().NewValue(value, length);
		mGeneration->push_back(v);
		return v;
	}
	Value* MemoryGC::NewBooleanValue(bool value)
	{
		return RawMemory().BooleanValue(value);
	}
	Value* MemoryGC::NewArrayValue(size_t row, size_t col, Value* fill)
	{
		auto v = RawMemory().NewValue(row, col, fill);
		mGeneration->push_back(v);
		return v;
	}

	void MemoryGC::Start(void)
	{
		Clean();
		mGenerationFullFlags[0] = false;
		mGenerationFullFlags[1] = false;
		mGenerationFullFlags[2] = false;
		mGenerationFullFlags[3] = false;

		mGeneration->reserve(1024 * 16);
		(mGeneration + 1)->reserve(1024 * 64);
		(mGeneration + 2)->reserve(1024 * 128);
		(mGeneration + 3)->reserve(1024 * 512);
	}
	void MemoryGC::Clean(void)
	{
		for (auto& g : mGeneration)
		{
			for (auto v : g)
			{
				RawMemory().FreeValue(v);
			}
			g.clear();
			g.reserve(0);
		}

		mMemoryPool.Clean();
	}








	MemoryAllocator::MemoryAllocator() :
		mMB0Count(0),
		mMB1Count(0),
		mMB2Count(0),
		mMB3Count(0),
		mMB4Count(0),
		mMB5Count(0),
		mMB0Head(nullptr),
		mMB1Head(nullptr),
		mMB2Head(nullptr),
		mMB3Head(nullptr),
		mMB4Head(nullptr),
		mMB5Head(nullptr)
	{

	}

	MemoryAllocator::~MemoryAllocator()
	{
		Clean();
	}

	Value* MemoryAllocator::mTrue = MemoryAllocator::InitBoolean(true);
	Value* MemoryAllocator::mFalse = MemoryAllocator::InitBoolean(false);

	Value* MemoryAllocator::InitBoolean(bool value)
	{
		auto pValue = reinterpret_cast<Value*>(new uint8_t[sizeof(Value)]);
		pValue->mValue.bValue = value;
		pValue->mType = Value::Boolean;
		pValue->mFlag = 0;
		return pValue;
	}

	Value* MemoryAllocator::BooleanValue(bool value)
	{
		return value ? mTrue : mFalse;
	}

	void* MemoryAllocator::AllocMemory(size_t size)
	{
		if (size <= sizeof(MemoryBlock0::data))
		{
			MemoryBlock0* mb = mMB0Head;
			if (mb == nullptr)
				mb = new MemoryBlock0();
			else
			{
				mMB0Head = mMB0Head->next;
				--mMB0Count;
			}
			mb->next = nullptr;
			return mb->data;
		}
		else if (size <= sizeof(MemoryBlock1::data))
		{
			MemoryBlock1* mb = mMB1Head;
			if (mb == nullptr)
				mb = new MemoryBlock1();
			else
			{
				mMB1Head = mMB1Head->next;
				--mMB1Count;
			}
			mb->next = nullptr;
			return mb->data;
		}
		else if (size <= sizeof(MemoryBlock2::data))
		{
			MemoryBlock2* mb = mMB2Head;
			if (mb == nullptr)
				mb = new MemoryBlock2();
			else
			{
				mMB2Head = mMB2Head->next;
				--mMB2Count;
			}
			mb->next = nullptr;
			return mb->data;
		}
		else if (size <= sizeof(MemoryBlock3::data))
		{
			MemoryBlock3* mb = mMB3Head;
			if (mb == nullptr)
				mb = new MemoryBlock3();
			else
			{
				mMB3Head = mMB3Head->next;
				--mMB3Count;
			}
			mb->next = nullptr;
			return mb->data;
		}
		else if (size <= sizeof(MemoryBlock4::data))
		{
			MemoryBlock4* mb = mMB4Head;
			if (mb == nullptr)
				mb = new MemoryBlock4();
			else
			{
				mMB4Head = mMB4Head->next;
				--mMB4Count;
			}
			mb->next = nullptr;
			return mb->data;
		}
		else if (size <= sizeof(MemoryBlock5::data))
		{
			MemoryBlock5* mb = mMB5Head;
			if (mb == nullptr)
				mb = new MemoryBlock5();
			else
			{
				mMB5Head = mMB5Head->next;
				--mMB5Count;
			}
			mb->next = nullptr;
			return mb->data;
		}
		return new uint8_t[size];
	}

	void MemoryAllocator::FreeMemory(void* p, size_t size)
	{
		if (size <= sizeof(MemoryBlock0::data))
		{
			MemoryBlock0* mb = reinterpret_cast<MemoryBlock0*>(reinterpret_cast<uint8_t*>(p) - sizeof(MemoryBlock0*));
			if (mMB0Count < 32 * 1024)
			{
				mb->next = mMB0Head;
				mMB0Head = mb;
				++mMB0Count;
			}
			else
			{
				delete mb;
			}
		}
		else if (size <= sizeof(MemoryBlock1::data))
		{
			MemoryBlock1* mb = reinterpret_cast<MemoryBlock1*>(reinterpret_cast<uint8_t*>(p) - sizeof(MemoryBlock1*));
			if (mMB1Count < 16 * 1024)
			{
				mb->next = mMB1Head;
				mMB1Head = mb;
				++mMB1Count;
			}
			else
			{
				delete mb;
			}
		}
		else if (size <= sizeof(MemoryBlock2::data))
		{
			MemoryBlock2* mb = reinterpret_cast<MemoryBlock2*>(reinterpret_cast<uint8_t*>(p) - sizeof(MemoryBlock2*));
			if (mMB2Count < 8 * 1024)
			{
				mb->next = mMB2Head;
				mMB2Head = mb;
				++mMB2Count;
			}
			else
			{
				delete mb;
			}
		}
		else if (size <= sizeof(MemoryBlock3::data))
		{
			MemoryBlock3* mb = reinterpret_cast<MemoryBlock3*>(reinterpret_cast<uint8_t*>(p) - sizeof(MemoryBlock3*));
			if (mMB3Count < 2 * 1024)
			{
				mb->next = mMB3Head;
				mMB3Head = mb;
				++mMB3Count;
			}
			else
			{
				delete mb;
			}
		}
		else if (size <= sizeof(MemoryBlock4::data))
		{
			MemoryBlock4* mb = reinterpret_cast<MemoryBlock4*>(reinterpret_cast<uint8_t*>(p) - sizeof(MemoryBlock4*));
			if (mMB4Count < 1024)
			{
				mb->next = mMB4Head;
				mMB4Head = mb;
				++mMB4Count;
			}
			else
			{
				delete mb;
			}
		}
		else if (size <= sizeof(MemoryBlock5::data))
		{
			MemoryBlock5* mb = reinterpret_cast<MemoryBlock5*>(reinterpret_cast<uint8_t*>(p) - sizeof(MemoryBlock5*));
			if (mMB5Count < 512)
			{
				mb->next = mMB5Head;
				mMB5Head = mb;
				++mMB5Count;
			}
			else
			{
				delete mb;
			}
		}
		else
		{
			delete[](reinterpret_cast<uint8_t*>(p));
		}
	}

	void MemoryAllocator::Clean(void)
	{
		{
			MemoryBlock0* mb = mMB0Head;
			while (mb != nullptr)
			{
				auto t = mb->next;
				delete mb;
				mb = t;
				--mMB0Count;
			}
			mMB0Head = nullptr;
			mMB0Count = 0;
		}

		{
			MemoryBlock1* mb = mMB1Head;
			while (mb != nullptr)
			{
				auto t = mb->next;
				delete mb;
				mb = t;
			}
			mMB1Head = nullptr;
			mMB1Count = 0;
		}

		{
			MemoryBlock2* mb = mMB2Head;
			while (mb != nullptr)
			{
				auto t = mb->next;
				delete mb;
				mb = t;
			}
			mMB2Head = nullptr;
			mMB2Count = 0;
		}

		{
			MemoryBlock3* mb = mMB3Head;
			while (mb != nullptr)
			{
				auto t = mb->next;
				delete mb;
				mb = t;
			}
			mMB3Head = nullptr;
			mMB3Count = 0;
		}

		{
			MemoryBlock4* mb = mMB4Head;
			while (mb != nullptr)
			{
				auto t = mb->next;
				delete mb;
				mb = t;
			}
			mMB4Head = nullptr;
			mMB4Count = 0;
		}

		{
			MemoryBlock5* mb = mMB5Head;
			while (mb != nullptr)
			{
				auto t = mb->next;
				delete mb;
				mb = t;
			}
			mMB5Head = nullptr;
			mMB5Count = 0;
		}
	}

	Value* MemoryAllocator::NewValue(int64_t value)
	{
		auto pValue = reinterpret_cast<Value*>(AllocMemory(sizeof(MemoryBlock0::data)));
		pValue->mValue.iValue = value;
		pValue->mType = Value::Integer;
		pValue->mFlag = 0;
		return pValue;
	}
	Value* MemoryAllocator::NewValue(double value)
	{
		auto pValue = reinterpret_cast<Value*>(AllocMemory(sizeof(MemoryBlock0::data)));
		pValue->mValue.dValue = value;
		pValue->mType = Value::Real;
		pValue->mFlag = 0;
		return pValue;
	}
	Value* MemoryAllocator::NewValue(const std::wstring& value)
	{
		return NewValue(value.data(), value.length());
	}
	Value* MemoryAllocator::NewValue(const wchar_t* value, size_t length)
	{
		if (value != nullptr && length == static_cast<size_t>(-1))
			length = std::wcslen(value);

		size_t baselen = ((size_t) &((Value *)0)->mValue.sValue.str);
		size_t datalen = (length + 1) * sizeof(wchar_t);
		auto pValue = reinterpret_cast<Value*>(AllocMemory(baselen + datalen));
		pValue->mValue.sValue.length = length;
		if (value != nullptr && length > 0)
			memcpy(pValue->mValue.sValue.str, value, length * sizeof(wchar_t));
		pValue->mType = Value::String;
		pValue->mFlag = 0;
		return pValue;
	}

	Value* MemoryAllocator::NewValue(size_t row, size_t col, Value* fill)
	{
		size_t count = row * col;
		const size_t baselen = ((size_t) &((Value *)0)->mValue.aValue.data);
		size_t datalen = (count) * sizeof(Value*);
		auto pValue = reinterpret_cast<Value*>(AllocMemory(baselen + datalen));
		pValue->mValue.aValue.row = row;
		pValue->mValue.aValue.col = col;
		if (fill == nullptr)
		{
			fill = BooleanValue(false);
		}

		for (size_t i = 0; i < count; ++i)
		{
			pValue->mValue.aValue.data[i] = fill;
		}
		pValue->mType = Value::Array;
		pValue->mFlag = 0;
		return pValue;
	}

	void MemoryAllocator::FreeValue(Value* value)
	{
		if (value == mTrue || value == mFalse)
			return;
		size_t size=0;
		switch (value->GetType())
		{
		case Value::Integer:
		case Value::Real:
		case Value::Boolean:
			size = sizeof(MemoryBlock0::data);
			break;
		case Value::String:
		{
			const size_t baselen = ((size_t) &((Value *)0)->mValue.sValue.str);
			size_t datalen = (value->mValue.sValue.length + 1) * sizeof(wchar_t);
			size = baselen + datalen;
		}
		break;
		case Value::Array:
		{
			size_t count = value->mValue.aValue.row * value->mValue.aValue.col;
			size_t baselen = ((size_t) &((Value *)0)->mValue.aValue.data);
			size_t datalen = (count) * sizeof(Value*);
			size = baselen + datalen;
		}
		break;
		default:
			assert(true);
			break;
		}
		FreeMemory(value, size);
	}
}
