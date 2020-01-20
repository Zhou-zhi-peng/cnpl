using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace cnpl
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var commandLine = new CommandLineParser();
                commandLine.Parse(args);

                if (commandLine.Has("help"))
                {
                    Console.WriteLine("编译器可使用下列参数：");
                    Console.Write("-S");
                    Console.WriteLine("\t[编译选项][必选]，输入的源程序路径。");
                    Console.WriteLine();

                    Console.Write("-RUN");
                    Console.WriteLine("\t[运行选项][可选]，使用该选项后，源程序将不会被编译（忽略编译参数），直接解析运行源程序。");
                    Console.WriteLine();

                    Console.Write("-ARCH");
                    Console.WriteLine("\t[编译选项][可选][默认值：x86]，选择目标运行平台的机器架构，可用架构系统：");
                    {
                        var files = Directory.GetFiles(assemblyPath, "*.Loader");
                        var archs = files.Select((s) =>
                        {
                            var temp = Path.GetFileNameWithoutExtension(s).Split('.');
                            if (temp.Length == 3)
                                return temp[2];
                            return string.Empty;
                        }).Distinct();
                        foreach (var a in archs)
                        {
                            Console.WriteLine($"\t\t{a}");
                        }
                    }
                    Console.WriteLine();

                    Console.Write("-OS");
                    Console.WriteLine("\t[编译选项][可选][默认值：windows]，选择目标运行平台的操作系统，可用系统名称：");
                    {
                        var files = Directory.GetFiles(assemblyPath, "*.Loader");
                        var archs = files.Select((s) =>
                        {
                            var temp = Path.GetFileNameWithoutExtension(s).Split('.');
                            if (temp.Length == 3)
                                return temp[1];
                            return string.Empty;
                        }).Distinct();
                        foreach (var a in archs)
                        {
                            Console.WriteLine($"\t\t{a}");
                        }
                    }
                    Console.WriteLine();

                    Console.Write("-I");
                    Console.WriteLine("\t[链接选项][可选][默认值：根据选择的OS和ARCH自动选择]，选择函数导入表文件，可用名称：");
                    {
                        var files = Directory.GetFiles(assemblyPath, "*.def");
                        var archs = files.Select((s) => Path.GetFileName(s)).Distinct();
                        foreach (var a in archs)
                        {
                            Console.WriteLine($"\t\t{a}");
                        }
                    }
                    Console.WriteLine();

                    Console.Write("-T");
                    Console.WriteLine("\t[链接选项][可选][默认值：EXE]，选择输出文件类型，可用类型：");
                    {
                        Console.WriteLine("\t\tASM\t字节码汇编文件。");
                        Console.WriteLine("\t\tBIN\t字节码文件。");
                        Console.WriteLine("\t\tEXE\t可执行文件。");
                    }
                    Console.WriteLine();

                    Console.Write("-O");
                    Console.WriteLine("\t[链接选项][可选][默认值：output]，选择输出文件路径。");
                    Console.WriteLine();


                    Console.Write("-help");
                    Console.WriteLine("\t[帮助选项][可选]，显示本页信息。");
                    Console.WriteLine();
                }
                else if (commandLine.Has("S"))
                {
                    var source = commandLine.GetValue("S", "");
                    if (!Path.IsPathRooted(source))
                        source = Path.GetFullPath(source);

                    Lexer lexer = new Lexer(new SourceInputStream(source));
                    GrammarParser parser = new GrammarParser();
                    var ast = parser.ParseProgram(lexer);

                    if (commandLine.Has("RUN"))
                    {
                        Console.Clear();
                        DemoActuator vm = new DemoActuator();
                        var vargs = new ArrayValue(args.Length, 1);
                        for(int i=0;i<args.Length;++i)//命令行参数
                        {
                            vargs.SetValue(i, 0, new StringValue(args[i]));
                        }
                        vm.GVariableTable["命令行参数"] = vargs;
                        ast.Execute(vm);
                    }
                    else
                    {
                        Compiler compiler = new Compiler();
                        var arch = commandLine.GetValue("ARCH", "x86");
                        var os = commandLine.GetValue("OS", "windows");
                        var importFile = commandLine.GetValue("I", $"import.{arch}.{os}.def");
                        var type = commandLine.GetValue("T", "EXE");
                        var output = commandLine.GetValue("O", "output");

                        if (!Path.IsPathRooted(importFile))
                            importFile = Path.Combine(assemblyPath, importFile);

                        if (!Path.IsPathRooted(output))
                            output = Path.Combine(Path.GetDirectoryName(source), output);

                        compiler.LoadImportDefine(importFile);
                        ast.Compile(compiler);

                        if (string.Compare(type, "ASM") == 0)
                        {
                            if (!Path.HasExtension(output))
                                output += ".asm";
                            compiler.LinkProgram(output, Compiler.OutputType.ASM, string.Empty);
                        }
                        else if (string.Compare(type, "BIN", true) == 0)
                        {
                            if (!Path.HasExtension(output))
                                output += ".bin";
                            compiler.LinkProgram(output, Compiler.OutputType.BIN, string.Empty);
                        }
                        else
                        {
                            if (!Path.HasExtension(output))
                            {
                                if (os == "windows")
                                    output += ".exe";
                                else if (os == "linux")
                                    output += ".elf";
                            }
                            string loaderPath = Path.Combine(assemblyPath, $"link.{os}.{arch}.Loader");
                            compiler.LinkProgram(output, Compiler.OutputType.EXE, loaderPath);
                        }
                    }
                }
                else
                {
                    throw new ArgumentNullException("S", "必需输入源程序路径.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
