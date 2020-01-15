#pragma once
#include <iostream>
#include <fstream>
#include <vector>
#include <list>
#include <string>
#include <cstdint>
#include <exception>
#include <unordered_map>

namespace VM
{
	class Engine;

	enum class InstructionID : uint16_t
	{
		NOOP = 0,
		ADD,
		AND,
		ALLOCDSTK,
		ARRAYMAKE,
		ARRAYREAD,
		ARRAYWRITE,
		CALL,
		CALLSYS,
		DIV,
		EQ,
		GT,
		JMP,
		JMPC,
		JMPN,
		LT,
		LC,
		LD,
		MOD,
		MUL,
		NE,
		NOT,
		OR,
		POP,
		PUSH,
		RET,
		SUB,
		SD
	};

	struct Value
	{
		friend class MemoryAllocator;
	public:
		typedef enum : uint8_t
		{
			Integer = 1,
			Real,
			String,
			Boolean,
			Array
		}Type;
	public:
		Value(void) = delete;
		Value(const Value&) = delete;
	public:
		Type GetType(void) const { return mType; }
		bool Is(Type type) const { return mType == type; }
		bool VEquals(const Value* value)const;

		bool AsBoolean(void) const;
		void AsString(std::wstring& s) const;
		std::wstring AsString(void)
		{
			std::wstring s;
			AsString(s);
			return s;
		}
		double AsReal(void) const;
		int64_t AsInteger(void) const;
	public:
		size_t GetRow(void)const;
		size_t GetCol(void)const;
		Value* GetValue(size_t r, size_t c) const;
		void SetValue(size_t r, size_t c, Value* v);
	public:
		void GCMarkSet(void)
		{
			mFlag |= 0x01;

			if (Is(Array))
			{
				if ((mFlag & 0x80) != 0)
					return;

				auto d = mValue.aValue.data;
				auto e = d+(mValue.aValue.row * mValue.aValue.col);
				mFlag |= 0x80;
				while (d < e)
				{
					(*d)->GCMarkSet();
					++d;
				}
				mFlag &= 0x7F;
			}
		}
		void GCMarkClear(void)
		{
			mFlag &= 0xFE;
		}
		bool IsGCMarked(void) const { return (mFlag & 0x01) != 0; }
		bool IsGCMarkFreed(void) const { return (mFlag & 0x01) == 0; }

		int GCMarkAddCount(void)
		{
			int r;
			mFlag &= 0xC0;
			r = ((mFlag >> 6) + 1);
			mFlag |= static_cast<uint16_t>(r << 6);
			return r;
		}

		void GCMarkClearCount(void)
		{
			mFlag &= 0xC0;
		}
	private:
		Type mType;
		uint8_t mReserved;
		uint16_t mFlag;
		union
		{
			int64_t iValue;
			double dValue;
			bool bValue;

			struct
			{
				size_t length;
				wchar_t str[2];
			}sValue;

			struct
			{
				size_t row;
				size_t col;
				Value* data[1];
			}aValue;
		} mValue;
	};

	class MemoryAllocator
	{
	public:
		MemoryAllocator() ;
		~MemoryAllocator();
	public:
		Value* NewValue(int64_t value);
		Value* NewValue(double value);
		Value* NewValue(const std::wstring& value);
		Value* NewValue(const wchar_t* value, size_t length);
		Value* NewValue(size_t row, size_t col, Value* fill = nullptr);
		void FreeValue(Value* value);
	public:
		void Clean(void);
	public:
		static Value* BooleanValue(bool value);
	private:
		static Value* InitBoolean(bool value);
	private:
		static Value* mTrue;
		static Value* mFalse;
	private:
		void* AllocMemory(size_t size);
		void FreeMemory(void*p, size_t size);
	private:
		struct MemoryBlock0
		{
			MemoryBlock0* next;
			uint8_t data[((size_t) &((Value *)0)->mValue.dValue)+sizeof(double)];
		};

		struct MemoryBlock1
		{
			MemoryBlock1* next;
			uint8_t data[sizeof(Value)];
		};

		struct MemoryBlock2
		{
			MemoryBlock2* next;
			uint8_t data[sizeof(Value) + 32];
		};

		struct MemoryBlock3
		{
			MemoryBlock3* next;
			uint8_t data[sizeof(Value) + 128];
		};

		struct MemoryBlock4
		{
			MemoryBlock4* next;
			uint8_t data[sizeof(Value) + 256];
		};

		struct MemoryBlock5
		{
			MemoryBlock5* next;
			uint8_t data[sizeof(Value) + 512];
		};

		size_t mMB0Count;
		size_t mMB1Count;
		size_t mMB2Count;
		size_t mMB3Count;
		size_t mMB4Count;
		size_t mMB5Count;
		MemoryBlock0* mMB0Head;
		MemoryBlock1* mMB1Head;
		MemoryBlock2* mMB2Head;
		MemoryBlock3* mMB3Head;
		MemoryBlock4* mMB4Head;
		MemoryBlock5* mMB5Head;
	};

	class MemoryGC
	{
	public:
		MemoryGC(const MemoryGC&) = delete;
		MemoryGC(void);
		~MemoryGC(void);
	public:
		void GC(Engine* engine);
		void CheckMemoryGC(Engine* engine);
		Value* NewIntegerValue(int32_t value);
		Value* NewIntegerValue(uint32_t value);
		Value* NewIntegerValue(int64_t value);
		Value* NewIntegerValue(uint64_t value);
		Value* NewRealValue(float value);
		Value* NewRealValue(double value);
		Value* NewStringValue(const std::wstring& value);
		Value* NewStringValue(const wchar_t* value, size_t length);
		Value* NewBooleanValue(bool value);
		Value* NewArrayValue(size_t row, size_t col, Value* fill = nullptr);

	public:
		void Start(void);
		void Clean(void);
	private:
		void GCMarkRoots(Engine* engine);
		void GCGenerationMarkClear(int gen);
		void GCGenerationClean(int gen);
	public:
		MemoryAllocator& RawMemory(void) { return mMemoryPool; }
	private:
		MemoryAllocator mMemoryPool;
		bool mGenerationFullFlags[4];
		std::vector<Value*> mGeneration[4];
	};

	typedef Value* (*PFN_HOST_CALL)(Engine* context, size_t argc, Value** argv);
	class Engine
	{
		friend class MemoryGC;
	private:
		typedef void (Engine::*InstructionFunc)(size_t tag);
		typedef struct
		{
			InstructionFunc func;
			size_t tag;
		}Instruction;
		typedef struct
		{
			const Instruction* ip;
			std::vector<Value*>* datastack;
		}CallNode;
	public:
		Engine();
		~Engine();
	public:
		void LoadProgram(std::fstream& in);
		int Run(void);
	public:
		MemoryGC& GC(void) { return mGC; }
	public:
		void SetGlobalVariable(const std::wstring& name, Value* value);
		Value* GetGlobalVariable(const std::wstring& name);

		size_t AppendHostCall(PFN_HOST_CALL);
	private:
		void ClearProgram(void);
		InstructionID ReadIID(std::fstream& in);
		template<class TNumber>
		TNumber ReadNumber(std::fstream& in);
		uint64_t Read7BitInt(std::fstream& in);
		void ReadString(std::fstream& in, std::wstring& value);
		bool ReadBoolean(std::fstream& in);
		void ReadBytes(std::fstream& in, void* buffer,size_t size);
		Value* ReadValue(std::fstream& in);
		void ReadInstruction(std::fstream& in, Instruction* instruction);
	private:
		void DATAStackAlloc(size_t size);
		Value* DATAStackGet(size_t index);
		void DATAStackPut(size_t index, Value* value);
		Value* CALCStackPop(void);
		void CALCStackPush(Value* value);
	private:
		void InstructionNOOP(size_t tag) {}
		void InstructionAND(size_t tag);
		void InstructionADD(size_t tag);
		void InstructionALLOCDSTK(size_t tag);
		void InstructionARRAYMAKE(size_t tag);
		void InstructionARRAYREAD(size_t tag);
		void InstructionARRAYWRITE(size_t tag);
		void InstructionCALL(size_t tag);
		void InstructionCALLSYS(size_t tag);
		void InstructionDIV(size_t tag);
		void InstructionEQ(size_t tag);
		void InstructionGT(size_t tag);
		void InstructionJMP(size_t tag);
		void InstructionJMPC(size_t tag);
		void InstructionJMPN(size_t tag);
		void InstructionLT(size_t tag);
		void InstructionLC(size_t tag);
		void InstructionLD(size_t tag);
		void InstructionMOD(size_t tag);
		void InstructionMUL(size_t tag);
		void InstructionNE(size_t tag);
		void InstructionNOT(size_t tag);
		void InstructionOR(size_t tag);
		void InstructionPOP(size_t tag);
		void InstructionPUSH(size_t tag);
		void InstructionRET(size_t tag);
		void InstructionSUB(size_t tag);
		void InstructionSD(size_t tag);
	private:
		std::vector<PFN_HOST_CALL> mHostCalls;
		std::vector<Value*> mConstants;
		size_t mInstructionCount;
		Instruction* mInstructions;
		const Instruction* mIP;
		std::vector<Value*> mCallParameters;
		std::vector<CallNode> mCallStack;
		std::vector<Value*> mCALCStack;
		std::vector<Value*>* mDATAStack;
		std::unordered_map<std::wstring, Value*> mGlobalVariableTable;
		MemoryGC mGC;
	};

	class Exception : public std::runtime_error
	{
	public:
		Exception(const Exception& value):
			std::runtime_error(value.what()),
			mFileName(value.mFileName),
			mFileLine(value.mFileLine),
			mErrorCode(value.mErrorCode)
		{

		}
		Exception(int errorCode, const std::string& message):
			std::runtime_error(message),
			mErrorCode(errorCode)
		{

		}
		Exception(int errorCode, const std::string& message, const std::wstring filename, size_t line) :
			Exception(errorCode, message)
		{
			mFileName = (filename);
			mFileLine = (line);
		}

		~Exception() = default;
	public:
		int ErrorCode(void)const { return mErrorCode; }
		const std::wstring& FileName(void)const { return mFileName; }
		size_t FileLine(void)const { return mFileLine; }
	private:
		std::wstring mFileName;
		size_t mFileLine;
		int mErrorCode;
	};
}
