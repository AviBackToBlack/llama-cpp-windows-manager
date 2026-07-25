using System.Globalization;

namespace LocalLlmConsole.Services;

/// <summary>Defines the value types supported by a llama-server flag.</summary>
public enum FlagValueType
{
    Boolean,
    Int,
    Double,
    String,
    Enum,
    File,
    Path,
    CommaList,
    PathList,
    ScaledPathList,
    MultiToken
}

/// <summary>Represents a single llama-server flag, its metadata, and validation rules.</summary>
public sealed record LlamaServerFlag(
    IReadOnlyList<string> Names,
    string Category,
    FlagValueType ValueType,
    object? Default = null,
    IReadOnlyList<string>? AllowedValues = null,
    double? Min = null,
    double? Max = null,
    string? Regex = null,
    string Description = "",
    bool IsSecurityCritical = false,
    int Arity = 1)
{
    public string PrimaryName => Names.Count > 0 ? Names[0] : "";

    public string? NegatedForm => LlamaServerFlagSchema.FindNegatedName(PrimaryName);

    public string UiLabel
    {
        get
        {
            var longFlag = Names.FirstOrDefault(n => n.StartsWith("--", StringComparison.Ordinal)) ?? PrimaryName;
            var label = longFlag.TrimStart('-').Replace('-', ' ');
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(label);
        }
    }
}

/// <summary>Static catalog of all known llama-server flags and lookup helpers.</summary>
public static class LlamaServerFlagSchema
{
    public static IReadOnlyList<LlamaServerFlag> All { get; } =
    [
        // Common params
        new LlamaServerFlag(["--help", "-h", "--usage"], "Basic", FlagValueType.Boolean, true, Description: "Print usage and exit."),
        new LlamaServerFlag(["--version"], "Basic", FlagValueType.Boolean, Description: "Show version and build info."),
        new LlamaServerFlag(["--cache-list", "-cl"], "Basic", FlagValueType.Boolean, Description: "Show list of models in cache."),
        new LlamaServerFlag(["--completion-bash"], "Basic", FlagValueType.Boolean, Description: "Print source-able bash completion script."),
        new LlamaServerFlag(["--threads", "-t"], "Basic", FlagValueType.Int, 0, Min: 0, Description: "Number of CPU threads to use during generation (0 = auto)."),
        new LlamaServerFlag(["--threads-batch", "-tb"], "Basic", FlagValueType.Int, 0, Min: 0, Description: "Number of threads to use during batch and prompt processing."),
        new LlamaServerFlag(["--cpu-mask", "-C"], "Basic", FlagValueType.String, Description: "CPU affinity mask: arbitrarily long hex."),
        new LlamaServerFlag(["--cpu-range", "-Cr"], "Basic", FlagValueType.String, Description: "Range of CPUs for affinity (lo-hi)."),
        new LlamaServerFlag(["--cpu-strict"], "Basic", FlagValueType.Enum, "0", AllowedValues: ["0", "1"], Description: "Use strict CPU placement."),
        new LlamaServerFlag(["--prio"], "Basic", FlagValueType.Int, 0, Min: -1, Max: 3, Description: "Process/thread priority."),
        new LlamaServerFlag(["--poll"], "Basic", FlagValueType.Int, 50, Min: 0, Max: 100, Description: "Polling level to wait for work (0 = no polling)."),
        new LlamaServerFlag(["--cpu-mask-batch", "-Cb"], "Basic", FlagValueType.String, Description: "CPU affinity mask for batch processing."),
        new LlamaServerFlag(["--cpu-range-batch", "-Crb"], "Basic", FlagValueType.String, Description: "Range of CPUs for affinity for batch processing."),
        new LlamaServerFlag(["--cpu-strict-batch"], "Basic", FlagValueType.Enum, "0", AllowedValues: ["0", "1"], Description: "Use strict CPU placement for batch processing."),
        new LlamaServerFlag(["--prio-batch"], "Basic", FlagValueType.Int, 0, Min: -1, Max: 3, Description: "Process/thread priority for batch processing."),
        new LlamaServerFlag(["--poll-batch"], "Basic", FlagValueType.Enum, "0", AllowedValues: ["0", "1"], Description: "Use polling to wait for batch work."),

        // Context and memory
        new LlamaServerFlag(["--ctx-size", "-c"], "Memory", FlagValueType.Int, 0, Min: 0, Description: "Size of the prompt context (0 = loaded from model)."),
        new LlamaServerFlag(["--predict", "-n", "--n-predict"], "Memory", FlagValueType.Int, -1, Min: -1, Description: "Number of tokens to predict (-1 = infinity)."),
        new LlamaServerFlag(["--batch-size", "-b"], "Memory", FlagValueType.Int, 2048, Min: 1, Description: "Logical maximum batch size."),
        new LlamaServerFlag(["--ubatch-size", "-ub"], "Memory", FlagValueType.Int, 512, Min: 1, Description: "Physical maximum batch size."),
        new LlamaServerFlag(["--keep"], "Memory", FlagValueType.Int, 0, Min: -1, Description: "Number of tokens to keep from the initial prompt."),
        new LlamaServerFlag(["--swa-full"], "Memory", FlagValueType.Boolean, false, Description: "Use full-size SWA cache."),
        new LlamaServerFlag(["--flash-attn", "-fa"], "Memory", FlagValueType.Boolean, "auto", Description: "Set Flash Attention use."),
        new LlamaServerFlag(["--perf"], "Basic", FlagValueType.Boolean, false, Description: "Enable internal libllama performance timings."),
        new LlamaServerFlag(["--no-perf"], "Basic", FlagValueType.Boolean, true, Description: "Disable internal libllama performance timings."),
        new LlamaServerFlag(["--escape", "-e"], "Basic", FlagValueType.Boolean, false, Description: "Process escape sequences."),
        new LlamaServerFlag(["--no-escape"], "Basic", FlagValueType.Boolean, false, Description: "Do not process escape sequences."),
        new LlamaServerFlag(["--rope-scaling"], "Memory", FlagValueType.Enum, "auto", AllowedValues: ["auto", "none", "linear", "yarn"], Description: "RoPE frequency scaling method."),
        new LlamaServerFlag(["--rope-scale"], "Memory", FlagValueType.Double, 0, Min: 0, Description: "RoPE context scaling factor."),
        new LlamaServerFlag(["--rope-freq-base"], "Memory", FlagValueType.Double, 0, Min: 0, Description: "RoPE base frequency."),
        new LlamaServerFlag(["--rope-freq-scale"], "Memory", FlagValueType.Double, 0, Min: 0, Description: "RoPE frequency scaling factor."),
        new LlamaServerFlag(["--yarn-orig-ctx"], "Memory", FlagValueType.Int, 0, Min: 0, Description: "YaRN: original context size of model."),
        new LlamaServerFlag(["--yarn-ext-factor"], "Memory", FlagValueType.Double, -1.0, Description: "YaRN: extrapolation mix factor."),
        new LlamaServerFlag(["--yarn-attn-factor"], "Memory", FlagValueType.Double, -1.0, Description: "YaRN: scale sqrt(t) or attention magnitude."),
        new LlamaServerFlag(["--yarn-beta-slow"], "Memory", FlagValueType.Double, -1.0, Description: "YaRN: high correction dim or alpha."),
        new LlamaServerFlag(["--yarn-beta-fast"], "Memory", FlagValueType.Double, -1.0, Description: "YaRN: low correction dim or beta."),
        new LlamaServerFlag(["--kv-offload", "-kvo"], "Memory", FlagValueType.Boolean, true, Description: "Enable KV cache offloading."),
        new LlamaServerFlag(["--no-kv-offload", "-nkvo"], "Memory", FlagValueType.Boolean, false, Description: "Disable KV cache offloading."),
        new LlamaServerFlag(["--repack"], "Memory", FlagValueType.Boolean, true, Description: "Enable weight repacking."),
        new LlamaServerFlag(["--no-repack", "-nr"], "Memory", FlagValueType.Boolean, false, Description: "Disable weight repacking."),
        new LlamaServerFlag(["--no-host"], "Memory", FlagValueType.Boolean, false, Description: "Bypass host buffer allowing extra buffers to be used."),
        new LlamaServerFlag(["--cache-type-k", "-ctk"], "Memory", FlagValueType.Enum, "f16", AllowedValues: ["f32", "f16", "bf16", "q8_0", "q4_0", "q4_1", "iq4_nl", "q5_0", "q5_1"], Description: "KV cache data type for K."),
        new LlamaServerFlag(["--cache-type-v", "-ctv"], "Memory", FlagValueType.Enum, "f16", AllowedValues: ["f32", "f16", "bf16", "q8_0", "q4_0", "q4_1", "iq4_nl", "q5_0", "q5_1"], Description: "KV cache data type for V."),
        new LlamaServerFlag(["--defrag-thold", "-dt"], "Memory", FlagValueType.Double, Description: "KV cache defragmentation threshold (deprecated)."),
        new LlamaServerFlag(["--rpc"], "Memory", FlagValueType.CommaList, Description: "Comma-separated list of RPC servers (host:port)."),
        new LlamaServerFlag(["--mlock"], "Memory", FlagValueType.Boolean, Description: "Force system to keep model in RAM."),
        new LlamaServerFlag(["--mmap"], "Memory", FlagValueType.Boolean, true, Description: "Memory-map model."),
        new LlamaServerFlag(["--no-mmap"], "Memory", FlagValueType.Boolean, false, Description: "Do not memory-map model."),
        new LlamaServerFlag(["--direct-io", "-dio"], "Memory", FlagValueType.Boolean, false, Description: "Use DirectIO if available."),
        new LlamaServerFlag(["--no-direct-io", "-ndio"], "Memory", FlagValueType.Boolean, false, Description: "Do not use DirectIO."),
        new LlamaServerFlag(["--numa"], "Memory", FlagValueType.Enum, "distribute", AllowedValues: ["distribute", "isolate", "cpunode", "interleave"], Description: "NUMA optimization strategy."),
        new LlamaServerFlag(["--device", "-dev"], "Memory", FlagValueType.CommaList, Description: "Comma-separated list of devices to use for offloading."),
        new LlamaServerFlag(["--list-devices"], "Memory", FlagValueType.Boolean, Description: "Print list of available devices and exit."),
        new LlamaServerFlag(["--override-tensor", "-ot"], "Memory", FlagValueType.CommaList, Description: "Override tensor buffer type (<pattern>=<type>)."),
        new LlamaServerFlag(["--cpu-moe", "-cmoe"], "Memory", FlagValueType.Boolean, false, Description: "Keep all MoE weights in CPU."),
        new LlamaServerFlag(["--n-cpu-moe", "-ncmoe"], "Memory", FlagValueType.Int, 0, Min: 0, Description: "Keep MoE weights of first N layers in CPU."),
        new LlamaServerFlag(["--gpu-layers", "--n-gpu-layers", "-ngl"], "Memory", FlagValueType.Int, 0, Min: 0, Description: "Max number of layers to store in VRAM."),
        new LlamaServerFlag(["--split-mode", "-sm"], "Memory", FlagValueType.Enum, "layer", AllowedValues: ["none", "layer", "row", "tensor"], Description: "How to split the model across multiple GPUs."),
        new LlamaServerFlag(["--tensor-split", "-ts"], "Memory", FlagValueType.CommaList, Description: "Fraction of model to offload to each GPU."),
        new LlamaServerFlag(["--main-gpu", "-mg"], "Memory", FlagValueType.Int, 0, Min: 0, Description: "The GPU to use for the model."),
        new LlamaServerFlag(["--fit", "-fit"], "Memory", FlagValueType.Enum, "on", AllowedValues: ["on", "off"], Description: "Adjust unset arguments to fit in device memory."),
        new LlamaServerFlag(["--fit-target", "-fitt"], "Memory", FlagValueType.CommaList, Description: "Target margin per device for --fit."),
        new LlamaServerFlag(["--fit-ctx", "-fitc"], "Memory", FlagValueType.Int, 4096, Min: 0, Description: "Minimum ctx size that can be set by --fit."),
        new LlamaServerFlag(["--check-tensors"], "Memory", FlagValueType.Boolean, false, Description: "Check model tensor data for invalid values."),
        new LlamaServerFlag(["--override-kv"], "Memory", FlagValueType.CommaList, Description: "Override model metadata by key (KEY=TYPE:VALUE)."),
        new LlamaServerFlag(["--op-offload"], "Memory", FlagValueType.Boolean, true, Description: "Offload host tensor operations to device."),
        new LlamaServerFlag(["--no-op-offload"], "Memory", FlagValueType.Boolean, false, Description: "Do not offload host tensor operations to device."),

        // LoRA and control vectors
        new LlamaServerFlag(["--lora"], "Model", FlagValueType.PathList, Description: "Path to LoRA adapter."),
        new LlamaServerFlag(["--lora-scaled"], "Model", FlagValueType.ScaledPathList, Description: "Path to LoRA adapter with scaling (FNAME:SCALE)."),
        new LlamaServerFlag(["--control-vector"], "Model", FlagValueType.PathList, Description: "Add a control vector."),
        new LlamaServerFlag(["--control-vector-scaled"], "Model", FlagValueType.ScaledPathList, Description: "Add a control vector with scaling."),
        new LlamaServerFlag(["--control-vector-layer-range"], "Model", FlagValueType.MultiToken, Arity: 2, Description: "Control vector layer range (START END)."),

        // Model loading
        new LlamaServerFlag(["--model", "-m"], "Model", FlagValueType.File, Description: "Model path to load."),
        new LlamaServerFlag(["--model-url", "-mu"], "Model", FlagValueType.String, Description: "Model download url."),
        new LlamaServerFlag(["--docker-repo", "-dr"], "Model", FlagValueType.String, Description: "Docker Hub model repository."),
        new LlamaServerFlag(["--hf-repo", "-hf", "-hfr"], "Model", FlagValueType.String, Description: "Hugging Face model repository."),
        new LlamaServerFlag(["--hf-file", "-hff"], "Model", FlagValueType.String, Description: "Hugging Face model file."),
        new LlamaServerFlag(["--hf-repo-v", "-hfv", "-hfrv"], "Model", FlagValueType.String, Description: "Hugging Face repository for vocoder model."),
        new LlamaServerFlag(["--hf-file-v", "-hffv"], "Model", FlagValueType.String, Description: "Hugging Face file for vocoder model."),
        new LlamaServerFlag(["--hf-token", "-hft"], "Model", FlagValueType.String, Description: "Hugging Face access token.", IsSecurityCritical: true),

        // Logging
        new LlamaServerFlag(["--log-disable"], "Logging", FlagValueType.Boolean, Description: "Disable logging."),
        new LlamaServerFlag(["--log-file"], "Logging", FlagValueType.File, Description: "Log to file."),
        new LlamaServerFlag(["--log-colors"], "Logging", FlagValueType.Enum, "auto", AllowedValues: ["on", "off", "auto"], Description: "Set colored logging."),
        new LlamaServerFlag(["--verbose", "-v", "--log-verbose"], "Logging", FlagValueType.Boolean, Description: "Set verbosity to infinity."),
        new LlamaServerFlag(["--offline"], "Basic", FlagValueType.Boolean, Description: "Offline mode."),
        new LlamaServerFlag(["--verbosity", "-lv", "--log-verbosity"], "Logging", FlagValueType.Int, 3, Min: 0, Description: "Set verbosity threshold."),
        new LlamaServerFlag(["--log-prefix"], "Logging", FlagValueType.Boolean, Description: "Enable prefix in log messages."),
        new LlamaServerFlag(["--no-log-prefix"], "Logging", FlagValueType.Boolean, Description: "Disable prefix in log messages."),
        new LlamaServerFlag(["--log-timestamps"], "Logging", FlagValueType.Boolean, Description: "Enable timestamps in log messages."),
        new LlamaServerFlag(["--no-log-timestamps"], "Logging", FlagValueType.Boolean, Description: "Disable timestamps in log messages."),
        new LlamaServerFlag(["--log-prompts-dir"], "Logging", FlagValueType.Path, Description: "Log prompts to directory."),

        // Speculative params
        new LlamaServerFlag(["--cache-type-k-draft", "--spec-draft-type-k", "-ctkd"], "Speculative", FlagValueType.Enum, "f16", AllowedValues: ["f32", "f16", "bf16", "q8_0", "q4_0", "q4_1", "iq4_nl", "q5_0", "q5_1"], Description: "KV cache data type for K for draft model."),
        new LlamaServerFlag(["--cache-type-v-draft", "--spec-draft-type-v", "-ctvd"], "Speculative", FlagValueType.Enum, "f16", AllowedValues: ["f32", "f16", "bf16", "q8_0", "q4_0", "q4_1", "iq4_nl", "q5_0", "q5_1"], Description: "KV cache data type for V for draft model."),
        new LlamaServerFlag(["--spec-draft-hf", "-hfd", "-hfrd", "--hf-repo-draft"], "Speculative", FlagValueType.String, Description: "Hugging Face repo for draft model."),
        new LlamaServerFlag(["--spec-draft-threads", "-td", "--threads-draft"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Threads for draft generation."),
        new LlamaServerFlag(["--spec-draft-threads-batch", "-tbd", "--threads-batch-draft"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Threads for draft batch processing."),
        new LlamaServerFlag(["--spec-draft-cpu-mask", "-Cd", "--cpu-mask-draft"], "Speculative", FlagValueType.String, Description: "Draft CPU affinity mask."),
        new LlamaServerFlag(["--spec-draft-cpu-range", "-Crd", "--cpu-range-draft"], "Speculative", FlagValueType.String, Description: "Draft CPU range."),
        new LlamaServerFlag(["--spec-draft-cpu-strict", "--cpu-strict-draft"], "Speculative", FlagValueType.Enum, "0", AllowedValues: ["0", "1"], Description: "Strict CPU placement for draft model."),
        new LlamaServerFlag(["--spec-draft-prio", "--prio-draft"], "Speculative", FlagValueType.Int, 0, Min: -1, Max: 3, Description: "Draft process/thread priority."),
        new LlamaServerFlag(["--spec-draft-poll", "--poll-draft"], "Speculative", FlagValueType.Enum, "0", AllowedValues: ["0", "1"], Description: "Polling for draft model work."),
        new LlamaServerFlag(["--spec-draft-cpu-mask-batch", "-Cbd", "--cpu-mask-batch-draft"], "Speculative", FlagValueType.String, Description: "Draft CPU affinity mask for batch."),
        new LlamaServerFlag(["--spec-draft-cpu-strict-batch", "--cpu-strict-batch-draft"], "Speculative", FlagValueType.Enum, "0", AllowedValues: ["0", "1"], Description: "Strict CPU placement for draft batch."),
        new LlamaServerFlag(["--spec-draft-prio-batch", "--prio-batch-draft"], "Speculative", FlagValueType.Int, 0, Min: -1, Max: 3, Description: "Draft priority for batch processing."),
        new LlamaServerFlag(["--spec-draft-poll-batch", "--poll-batch-draft"], "Speculative", FlagValueType.Enum, "0", AllowedValues: ["0", "1"], Description: "Polling for draft batch work."),
        new LlamaServerFlag(["--spec-draft-override-tensor", "-otd", "--override-tensor-draft"], "Speculative", FlagValueType.CommaList, Description: "Override tensor buffer type for draft model."),
        new LlamaServerFlag(["--spec-draft-cpu-moe", "-cmoed", "--cpu-moe-draft"], "Speculative", FlagValueType.Boolean, false, Description: "Keep all MoE weights in CPU for draft model."),
        new LlamaServerFlag(["--spec-draft-n-cpu-moe", "--spec-draft-ncmoe", "-ncmoed", "--n-cpu-moe-draft"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Keep MoE weights of first N layers in CPU for draft."),
        new LlamaServerFlag(["--spec-draft-n-max"], "Speculative", FlagValueType.Int, 3, Min: 0, Description: "Number of tokens to draft for speculative decoding."),
        new LlamaServerFlag(["--spec-draft-n-min"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Minimum number of draft tokens to use."),
        new LlamaServerFlag(["--spec-draft-p-split", "--draft-p-split"], "Speculative", FlagValueType.Double, 0.1, Min: 0, Max: 1, Description: "Speculative decoding split probability."),
        new LlamaServerFlag(["--spec-draft-p-min", "--draft-p-min"], "Speculative", FlagValueType.Double, 0.0, Min: 0, Max: 1, Description: "Minimum speculative decoding probability."),
        new LlamaServerFlag(["--spec-draft-backend-sampling"], "Speculative", FlagValueType.Boolean, true, Description: "Offload draft sampling to backend."),
        new LlamaServerFlag(["--no-spec-draft-backend-sampling"], "Speculative", FlagValueType.Boolean, false, Description: "Do not offload draft sampling to backend."),
        new LlamaServerFlag(["--spec-draft-device", "-devd", "--device-draft"], "Speculative", FlagValueType.CommaList, Description: "Devices for offloading draft model."),
        new LlamaServerFlag(["--spec-draft-ngl", "-ngld", "--gpu-layers-draft", "--n-gpu-layers-draft"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Max draft model layers in VRAM."),
        new LlamaServerFlag(["--spec-draft-model", "-md", "--model-draft"], "Speculative", FlagValueType.File, Description: "Draft model for speculative decoding."),
        new LlamaServerFlag(["--mtp-head"], "Speculative", FlagValueType.File, Description: "Path to atomic MTP head model for atomic-mtp speculative decoding."),
        new LlamaServerFlag(["--spec-type"], "Speculative", FlagValueType.CommaList, Description: "Comma-separated list of speculative decoding types.", AllowedValues: ["none", "draft-simple", "draft-eagle3", "draft-mtp", "draft-dflash", "ngram-simple", "ngram-map-k", "ngram-map-k4v", "ngram-mod", "ngram-cache"]),
        new LlamaServerFlag(["--spec-ngram-mod-n-min"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Minimum ngram tokens for ngram-mod speculative."),
        new LlamaServerFlag(["--spec-ngram-mod-n-max"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Maximum ngram tokens for ngram-mod speculative."),
        new LlamaServerFlag(["--spec-ngram-mod-n-match"], "Speculative", FlagValueType.Int, 24, Min: 0, Description: "Ngram-mod lookup length."),
        new LlamaServerFlag(["--spec-ngram-simple-size-n"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Ngram size N for ngram-simple."),
        new LlamaServerFlag(["--spec-ngram-simple-size-m"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Ngram size M for ngram-simple."),
        new LlamaServerFlag(["--spec-ngram-simple-min-hits"], "Speculative", FlagValueType.Int, 1, Min: 0, Description: "Minimum hits for ngram-simple."),
        new LlamaServerFlag(["--spec-ngram-map-k-size-n"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Ngram size N for ngram-map-k."),
        new LlamaServerFlag(["--spec-ngram-map-k-size-m"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Ngram size M for ngram-map-k."),
        new LlamaServerFlag(["--spec-ngram-map-k-min-hits"], "Speculative", FlagValueType.Int, 1, Min: 0, Description: "Minimum hits for ngram-map-k."),
        new LlamaServerFlag(["--spec-ngram-map-k4v-size-n"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Ngram size N for ngram-map-k4v."),
        new LlamaServerFlag(["--spec-ngram-map-k4v-size-m"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Ngram size M for ngram-map-k4v."),
        new LlamaServerFlag(["--spec-ngram-map-k4v-min-hits"], "Speculative", FlagValueType.Int, 1, Min: 0, Description: "Minimum hits for ngram-map-k4v."),
        new LlamaServerFlag(["--spec-ngram-size-n"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Deprecated: use respective per-spec flag."),
        new LlamaServerFlag(["--spec-ngram-size-m"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Deprecated: use respective per-spec flag."),
        new LlamaServerFlag(["--spec-ngram-min-hits"], "Speculative", FlagValueType.Int, 0, Min: 0, Description: "Deprecated: use respective per-spec flag."),
        new LlamaServerFlag(["--draft", "--draft-n", "--draft-max"], "Speculative", FlagValueType.Int, Description: "Removed. Use --spec-draft-n-max or --spec-ngram-mod-n-max."),
        new LlamaServerFlag(["--draft-min", "--draft-n-min"], "Speculative", FlagValueType.Int, Description: "Removed. Use --spec-draft-n-min or --spec-ngram-mod-n-min."),

        // Sampling params
        new LlamaServerFlag(["--samplers"], "Sampling", FlagValueType.CommaList, Description: "Samplers used for generation in order."),
        new LlamaServerFlag(["--seed", "-s"], "Sampling", FlagValueType.Int, -1, Min: -1, Description: "RNG seed (-1 = random)."),
        new LlamaServerFlag(["--sampler-seq", "--sampling-seq"], "Sampling", FlagValueType.CommaList, Description: "Simplified sequence for samplers."),
        new LlamaServerFlag(["--ignore-eos"], "Sampling", FlagValueType.Boolean, false, Description: "Ignore end of stream token and continue generating."),
        new LlamaServerFlag(["--temp", "--temperature"], "Sampling", FlagValueType.Double, 0.8, Min: 0, Max: 10, Description: "Temperature."),
        new LlamaServerFlag(["--top-k"], "Sampling", FlagValueType.Int, 40, Min: 0, Max: 100000, Description: "Top-k sampling (0 = disabled)."),
        new LlamaServerFlag(["--top-p"], "Sampling", FlagValueType.Double, 0.95, Min: 0, Max: 1, Description: "Top-p sampling (1.0 = disabled)."),
        new LlamaServerFlag(["--min-p"], "Sampling", FlagValueType.Double, 0.05, Min: 0, Max: 1, Description: "Min-p sampling (0.0 = disabled)."),
        new LlamaServerFlag(["--top-nsigma", "--top-n-sigma"], "Sampling", FlagValueType.Double, -1.0, Description: "Top-n-sigma sampling."),
        new LlamaServerFlag(["--xtc-probability"], "Sampling", FlagValueType.Double, 0.0, Min: 0, Max: 1, Description: "XTC probability."),
        new LlamaServerFlag(["--xtc-threshold"], "Sampling", FlagValueType.Double, 0.1, Min: 0, Max: 1, Description: "XTC threshold."),
        new LlamaServerFlag(["--typical", "--typical-p"], "Sampling", FlagValueType.Double, 1.0, Min: 0, Max: 1, Description: "Locally typical sampling."),
        new LlamaServerFlag(["--repeat-last-n"], "Sampling", FlagValueType.Int, 64, Min: -1, Description: "Last n tokens to consider for penalize."),
        new LlamaServerFlag(["--repeat-penalty"], "Sampling", FlagValueType.Double, 1.0, Min: 0, Max: 10, Description: "Penalize repeat sequence of tokens."),
        new LlamaServerFlag(["--presence-penalty"], "Sampling", FlagValueType.Double, 0.0, Min: -10, Max: 10, Description: "Repeat alpha presence penalty."),
        new LlamaServerFlag(["--frequency-penalty"], "Sampling", FlagValueType.Double, 0.0, Min: -10, Max: 10, Description: "Repeat alpha frequency penalty."),
        new LlamaServerFlag(["--dry-multiplier"], "Sampling", FlagValueType.Double, 0.0, Min: 0, Description: "DRY sampling multiplier."),
        new LlamaServerFlag(["--dry-base"], "Sampling", FlagValueType.Double, 1.75, Min: 0, Description: "DRY sampling base value."),
        new LlamaServerFlag(["--dry-allowed-length"], "Sampling", FlagValueType.Int, 2, Min: 0, Description: "Allowed length for DRY sampling."),
        new LlamaServerFlag(["--dry-penalty-last-n"], "Sampling", FlagValueType.Int, -1, Min: -1, Description: "DRY penalty for last n tokens."),
        new LlamaServerFlag(["--dry-sequence-breaker"], "Sampling", FlagValueType.String, Description: "Sequence breaker for DRY sampling."),
        new LlamaServerFlag(["--adaptive-target"], "Sampling", FlagValueType.Double, -1.0, Min: -1, Max: 1, Description: "Adaptive-p target probability."),
        new LlamaServerFlag(["--adaptive-decay"], "Sampling", FlagValueType.Double, 0.90, Min: 0, Description: "Adaptive-p decay rate."),
        new LlamaServerFlag(["--dynatemp-range"], "Sampling", FlagValueType.Double, 0.0, Min: 0, Description: "Dynamic temperature range."),
        new LlamaServerFlag(["--dynatemp-exp"], "Sampling", FlagValueType.Double, 1.0, Min: 0, Description: "Dynamic temperature exponent."),
        new LlamaServerFlag(["--mirostat"], "Sampling", FlagValueType.Int, 0, Min: 0, Max: 2, Description: "Mirostat sampling mode."),
        new LlamaServerFlag(["--mirostat-lr"], "Sampling", FlagValueType.Double, 0.1, Min: 0, Description: "Mirostat learning rate."),
        new LlamaServerFlag(["--mirostat-ent"], "Sampling", FlagValueType.Double, 5.0, Min: 0, Description: "Mirostat target entropy."),
        new LlamaServerFlag(["--logit-bias", "-l"], "Sampling", FlagValueType.CommaList, Description: "Modifies likelihood of token appearing."),
        new LlamaServerFlag(["--grammar"], "Sampling", FlagValueType.String, Description: "BNF-like grammar to constrain generations."),
        new LlamaServerFlag(["--grammar-file"], "Sampling", FlagValueType.File, Description: "File to read grammar from."),
        new LlamaServerFlag(["--json-schema", "-j"], "Sampling", FlagValueType.String, Description: "JSON schema to constrain generations."),
        new LlamaServerFlag(["--json-schema-file", "-jf"], "Sampling", FlagValueType.File, Description: "File containing a JSON schema."),
        new LlamaServerFlag(["--backend-sampling", "-bs"], "Sampling", FlagValueType.Boolean, false, Description: "Enable backend sampling (experimental)."),

        // Example-specific / server params
        new LlamaServerFlag(["--lookup-cache-static", "-lcs"], "Server", FlagValueType.File, Description: "Path to static lookup cache."),
        new LlamaServerFlag(["--lookup-cache-dynamic", "-lcd"], "Server", FlagValueType.File, Description: "Path to dynamic lookup cache."),
        new LlamaServerFlag(["--ctx-checkpoints", "-ctxcp", "--swa-checkpoints"], "Memory", FlagValueType.Int, null, Min: 0, Description: "Max number of context checkpoints per slot."),
        new LlamaServerFlag(["--checkpoint-min-step", "-cms"], "Memory", FlagValueType.Int, 8192, Min: -1, Description: "Minimum spacing between context checkpoints."),
        new LlamaServerFlag(["--cache-ram", "-cram"], "Memory", FlagValueType.Int, 8192, Min: -1, Description: "Maximum cache size in MiB (-1 = no limit, 0 = disable)."),
        new LlamaServerFlag(["--kv-unified", "-kvu"], "Memory", FlagValueType.Boolean, true, Description: "Use single unified KV buffer."),
        new LlamaServerFlag(["--no-kv-unified", "-no-kvu"], "Memory", FlagValueType.Boolean, false, Description: "Do not use single unified KV buffer."),
        new LlamaServerFlag(["--cache-idle-slots"], "Memory", FlagValueType.Boolean, true, Description: "Save idle slots to prompt cache."),
        new LlamaServerFlag(["--no-cache-idle-slots"], "Memory", FlagValueType.Boolean, false, Description: "Do not save idle slots to prompt cache."),
        new LlamaServerFlag(["--context-shift"], "Server", FlagValueType.Boolean, false, Description: "Use context shift on infinite text generation."),
        new LlamaServerFlag(["--no-context-shift"], "Server", FlagValueType.Boolean, false, Description: "Do not use context shift."),
        new LlamaServerFlag(["--reverse-prompt", "-r"], "Server", FlagValueType.String, Description: "Halt generation at PROMPT in interactive mode."),
        new LlamaServerFlag(["--special", "-sp"], "Server", FlagValueType.Boolean, false, Description: "Special tokens output enabled."),
        new LlamaServerFlag(["--warmup"], "Server", FlagValueType.Boolean, true, Description: "Perform warmup with an empty run."),
        new LlamaServerFlag(["--no-warmup"], "Server", FlagValueType.Boolean, false, Description: "Do not perform warmup."),
        new LlamaServerFlag(["--spm-infill"], "Server", FlagValueType.Boolean, false, Description: "Use Suffix/Prefix/Middle pattern for infill."),
        new LlamaServerFlag(["--pooling"], "Server", FlagValueType.Enum, "none", AllowedValues: ["none", "mean", "cls", "last", "rank"], Description: "Pooling type for embeddings."),
        new LlamaServerFlag(["--parallel", "-np"], "Server", FlagValueType.Int, -1, Min: -1, Description: "Number of server slots (-1 = auto)."),
        new LlamaServerFlag(["--cont-batching", "-cb"], "Server", FlagValueType.Boolean, true, Description: "Enable continuous batching."),
        new LlamaServerFlag(["--no-cont-batching", "-nocb"], "Server", FlagValueType.Boolean, false, Description: "Disable continuous batching."),
        new LlamaServerFlag(["--mmproj", "-mm"], "Vision", FlagValueType.File, Description: "Path to a multimodal projector file."),
        new LlamaServerFlag(["--mmproj-url", "-mmu"], "Vision", FlagValueType.String, Description: "URL to a multimodal projector file."),
        new LlamaServerFlag(["--mmproj-auto"], "Vision", FlagValueType.Boolean, true, Description: "Use multimodal projector file if available."),
        new LlamaServerFlag(["--no-mmproj"], "Vision", FlagValueType.Boolean, false, Description: "Do not use multimodal projector."),
        new LlamaServerFlag(["--no-mmproj-auto"], "Vision", FlagValueType.Boolean, false, Description: "Do not auto-use multimodal projector."),
        new LlamaServerFlag(["--mmproj-offload"], "Vision", FlagValueType.Boolean, true, Description: "Enable GPU offloading for multimodal projector."),
        new LlamaServerFlag(["--no-mmproj-offload"], "Vision", FlagValueType.Boolean, false, Description: "Disable GPU offloading for multimodal projector."),
        new LlamaServerFlag(["--image-min-tokens"], "Vision", FlagValueType.Int, 0, Min: 0, Description: "Minimum image tokens for vision models."),
        new LlamaServerFlag(["--image-max-tokens"], "Vision", FlagValueType.Int, 0, Min: 0, Description: "Maximum image tokens for vision models."),
        new LlamaServerFlag(["--mtmd-batch-max-tokens"], "Vision", FlagValueType.Int, 1024, Min: 0, Description: "Maximum image tokens per batch."),
        new LlamaServerFlag(["--alias", "-a"], "Server", FlagValueType.String, Description: "Set model name aliases (comma-separated)."),
        new LlamaServerFlag(["--tags"], "Server", FlagValueType.CommaList, Description: "Set model tags (comma-separated)."),
        new LlamaServerFlag(["--embd-normalize"], "Server", FlagValueType.Int, 2, Min: -1, Description: "Normalisation for embeddings."),
        new LlamaServerFlag(["--host"], "Server", FlagValueType.String, "127.0.0.1", Description: "IP address to listen on (or UNIX socket path).", IsSecurityCritical: true),
        new LlamaServerFlag(["--port"], "Server", FlagValueType.Int, 8080, Min: 1, Max: 65535, Description: "Port to listen on.", IsSecurityCritical: true),
        new LlamaServerFlag(["--reuse-port"], "Server", FlagValueType.Boolean, false, Description: "Allow multiple sockets to bind to the same port."),
        new LlamaServerFlag(["--path"], "Server", FlagValueType.Path, Description: "Path to serve static files from."),
        new LlamaServerFlag(["--api-prefix"], "Server", FlagValueType.String, Description: "Prefix path the server serves from."),
        new LlamaServerFlag(["--ui-config", "--webui-config"], "Server", FlagValueType.String, Description: "JSON that provides default UI settings."),
        new LlamaServerFlag(["--ui-config-file", "--webui-config-file"], "Server", FlagValueType.File, Description: "JSON file that provides default UI settings."),
        new LlamaServerFlag(["--ui-mcp-proxy", "--webui-mcp-proxy"], "Server", FlagValueType.Boolean, false, Description: "Enable MCP CORS proxy (experimental).", IsSecurityCritical: true),
        new LlamaServerFlag(["--no-ui-mcp-proxy", "--no-webui-mcp-proxy"], "Server", FlagValueType.Boolean, false, Description: "Disable MCP CORS proxy."),
        new LlamaServerFlag(["--tools"], "Server", FlagValueType.CommaList, Description: "Enable built-in tools for AI agents (experimental).", IsSecurityCritical: true),
        new LlamaServerFlag(["--agent", "-ag"], "Server", FlagValueType.Boolean, false, Description: "Enable CORS proxy and all built-in tools.", IsSecurityCritical: true),
        new LlamaServerFlag(["--no-agent", "-no-ag"], "Server", FlagValueType.Boolean, false, Description: "Disable agent features."),
        new LlamaServerFlag(["--ui", "--webui"], "Server", FlagValueType.Boolean, true, Description: "Enable the Web UI."),
        new LlamaServerFlag(["--no-ui", "--no-webui"], "Server", FlagValueType.Boolean, false, Description: "Disable the Web UI."),
        new LlamaServerFlag(["--embedding", "--embeddings"], "Server", FlagValueType.Boolean, false, Description: "Restrict to only embedding use case."),
        new LlamaServerFlag(["--rerank", "--reranking"], "Server", FlagValueType.Boolean, false, Description: "Enable reranking endpoint."),
        new LlamaServerFlag(["--api-key"], "Server", FlagValueType.String, Description: "API key to use for authentication.", IsSecurityCritical: true),
        new LlamaServerFlag(["--api-key-file"], "Server", FlagValueType.File, Description: "Path to file containing API keys.", IsSecurityCritical: true),
        new LlamaServerFlag(["--load-mode"], "Server", FlagValueType.Enum, "mmap", AllowedValues: ["none", "mmap", "mlock", "dio"], Description: "Model loading mode."),
        new LlamaServerFlag(["--ssl-key-file"], "Server", FlagValueType.File, Description: "Path to PEM-encoded SSL private key."),
        new LlamaServerFlag(["--ssl-cert-file"], "Server", FlagValueType.File, Description: "Path to PEM-encoded SSL certificate."),
        new LlamaServerFlag(["--chat-template-kwargs"], "Server", FlagValueType.String, Description: "Additional params for JSON template parser."),
        new LlamaServerFlag(["--timeout", "-to"], "Server", FlagValueType.Int, 3600, Min: 0, Description: "Server read/write timeout in seconds."),
        new LlamaServerFlag(["--sse-ping-interval"], "Server", FlagValueType.Int, 30, Min: -1, Description: "Server SSE ping interval in seconds."),
        new LlamaServerFlag(["--threads-http"], "Server", FlagValueType.Int, -1, Min: -1, Description: "Number of threads to process HTTP requests."),
        new LlamaServerFlag(["--cache-prompt"], "Server", FlagValueType.Boolean, true, Description: "Enable prompt caching."),
        new LlamaServerFlag(["--no-cache-prompt"], "Server", FlagValueType.Boolean, false, Description: "Disable prompt caching."),
        new LlamaServerFlag(["--cache-reuse"], "Server", FlagValueType.Int, 0, Min: 0, Description: "Min chunk size to reuse from cache via KV shifting."),
        new LlamaServerFlag(["--metrics"], "Server", FlagValueType.Boolean, false, Description: "Enable Prometheus compatible metrics endpoint."),
        new LlamaServerFlag(["--props"], "Server", FlagValueType.Boolean, false, Description: "Enable changing global properties via POST /props."),
        new LlamaServerFlag(["--slots"], "Server", FlagValueType.Boolean, true, Description: "Expose slots monitoring endpoint."),
        new LlamaServerFlag(["--no-slots"], "Server", FlagValueType.Boolean, false, Description: "Do not expose slots monitoring endpoint."),
        new LlamaServerFlag(["--slot-save-path"], "Server", FlagValueType.Path, Description: "Path to save slot KV cache."),
        new LlamaServerFlag(["--media-path"], "Server", FlagValueType.Path, Description: "Directory for loading local media files."),
        new LlamaServerFlag(["--models-dir"], "Server", FlagValueType.Path, Description: "Directory containing models for router server."),
        new LlamaServerFlag(["--models-preset"], "Server", FlagValueType.File, Description: "Path to INI file containing model presets."),
        new LlamaServerFlag(["--models-max"], "Server", FlagValueType.Int, 4, Min: 0, Description: "Max number of models to load simultaneously for router."),
        new LlamaServerFlag(["--models-autoload"], "Server", FlagValueType.Boolean, true, Description: "Automatically load models for router."),
        new LlamaServerFlag(["--no-models-autoload"], "Server", FlagValueType.Boolean, false, Description: "Do not auto-load models for router."),
        new LlamaServerFlag(["--jinja"], "Server", FlagValueType.Boolean, true, Description: "Use jinja template engine for chat."),
        new LlamaServerFlag(["--no-jinja"], "Server", FlagValueType.Boolean, false, Description: "Do not use jinja template engine."),
        new LlamaServerFlag(["--reasoning", "-rea"], "Server", FlagValueType.Enum, "auto", AllowedValues: ["auto", "on", "off"], Description: "Use reasoning/thinking in the chat."),
        new LlamaServerFlag(["--reasoning-format"], "Server", FlagValueType.Enum, "auto", AllowedValues: ["auto", "none", "deepseek", "deepseek-legacy"], Description: "Controls thought tags extraction format."),
        new LlamaServerFlag(["--reasoning-budget"], "Server", FlagValueType.Int, -1, Min: -1, Description: "Token budget for thinking."),
        new LlamaServerFlag(["--reasoning-budget-message"], "Server", FlagValueType.String, Description: "Message injected before end-of-thinking tag."),
        new LlamaServerFlag(["--reasoning-preserve"], "Server", FlagValueType.Boolean, true, Description: "Preserve reasoning trace in full history."),
        new LlamaServerFlag(["--no-reasoning-preserve"], "Server", FlagValueType.Boolean, false, Description: "Do not preserve reasoning trace."),
        new LlamaServerFlag(["--chat-template"], "Server", FlagValueType.String, Description: "Set custom jinja chat template."),
        new LlamaServerFlag(["--chat-template-file"], "Server", FlagValueType.File, Description: "Set custom jinja chat template file."),
        new LlamaServerFlag(["--skip-chat-parsing"], "Server", FlagValueType.Boolean, false, Description: "Force pure content parser even if Jinja template is specified."),
        new LlamaServerFlag(["--no-skip-chat-parsing"], "Server", FlagValueType.Boolean, false, Description: "Do not force pure content parser."),
        new LlamaServerFlag(["--prefill-assistant"], "Server", FlagValueType.Boolean, false, Description: "Prefill assistant's response."),
        new LlamaServerFlag(["--no-prefill-assistant"], "Server", FlagValueType.Boolean, false, Description: "Do not prefill assistant's response."),
        new LlamaServerFlag(["--slot-prompt-similarity", "-sps"], "Server", FlagValueType.Double, 0.10, Min: 0, Max: 1, Description: "Prompt similarity threshold for slot reuse."),
        new LlamaServerFlag(["--lora-init-without-apply"], "Server", FlagValueType.Boolean, false, Description: "Load LoRA adapters without applying them."),
        new LlamaServerFlag(["--sleep-idle-seconds"], "Server", FlagValueType.Int, -1, Min: -1, Description: "Seconds of idleness after which server sleeps."),
        new LlamaServerFlag(["--model-vocoder", "-mv"], "Server", FlagValueType.File, Description: "Vocoder model for audio generation."),
        new LlamaServerFlag(["--tts-use-guide-tokens"], "Server", FlagValueType.Boolean, false, Description: "Use guide tokens to improve TTS word recall."),
        new LlamaServerFlag(["--embd-gemma-default"], "Server", FlagValueType.Boolean, false, Description: "Use default EmbeddingGemma model."),
        new LlamaServerFlag(["--fim-qwen-1.5b-default"], "Server", FlagValueType.Boolean, false, Description: "Use default Qwen 2.5 Coder 1.5B."),
        new LlamaServerFlag(["--fim-qwen-3b-default"], "Server", FlagValueType.Boolean, false, Description: "Use default Qwen 2.5 Coder 3B."),
        new LlamaServerFlag(["--fim-qwen-7b-default"], "Server", FlagValueType.Boolean, false, Description: "Use default Qwen 2.5 Coder 7B."),
        new LlamaServerFlag(["--fim-qwen-7b-spec"], "Server", FlagValueType.Boolean, false, Description: "Use Qwen 2.5 Coder 7B + 0.5B draft."),
        new LlamaServerFlag(["--fim-qwen-14b-spec"], "Server", FlagValueType.Boolean, false, Description: "Use Qwen 2.5 Coder 14B + 0.5B draft."),
        new LlamaServerFlag(["--fim-qwen-30b-default"], "Server", FlagValueType.Boolean, false, Description: "Use default Qwen 3 Coder 30B A3B Instruct."),
        new LlamaServerFlag(["--gpt-oss-20b-default"], "Server", FlagValueType.Boolean, false, Description: "Use gpt-oss-20b."),
        new LlamaServerFlag(["--gpt-oss-120b-default"], "Server", FlagValueType.Boolean, false, Description: "Use gpt-oss-120b."),
        new LlamaServerFlag(["--vision-gemma-4b-default"], "Server", FlagValueType.Boolean, false, Description: "Use Gemma 3 4B QAT."),
        new LlamaServerFlag(["--vision-gemma-12b-default"], "Server", FlagValueType.Boolean, false, Description: "Use Gemma 3 12B QAT."),
        new LlamaServerFlag(["--spec-default"], "Server", FlagValueType.Boolean, false, Description: "Enable default speculative decoding config."),
    ];

    private static readonly Dictionary<string, LlamaServerFlag> _byName = CreateNameIndex();

    private static Dictionary<string, LlamaServerFlag> CreateNameIndex()
    {
        var index = new Dictionary<string, LlamaServerFlag>(StringComparer.Ordinal);
        foreach (var flag in All)
        {
            foreach (var name in flag.Names)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                index[name] = flag;
            }
        }
        return index;
    }

    public static LlamaServerFlag? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (_byName.TryGetValue(name, out var flag)) return flag;

        // Long flags are case-insensitive; short flags are case-sensitive.
        if (!name.StartsWith("--", StringComparison.OrdinalIgnoreCase)) return null;
        foreach (var schemaFlag in All)
        {
            foreach (var n in schemaFlag.Names)
            {
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                    return schemaFlag;
            }
        }

        return null;
    }

    public static bool IsKnownFlag(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var name = token.Trim();
        if (name.StartsWith("--no-", StringComparison.OrdinalIgnoreCase))
            name = "--" + name[5..];
        if (!name.StartsWith('-')) return false;
        return FindByName(name) is not null;
    }

    private static readonly Dictionary<string, string> _negatedPairs = CreateNegatedPairs();

    private static Dictionary<string, string> CreateNegatedPairs()
    {
        var pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var flag in All)
        {
            if (!flag.PrimaryName.StartsWith("--", StringComparison.Ordinal)) continue;
            var positive = flag.PrimaryName;
            if (positive.StartsWith("--no-", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = "--" + positive[5..];
                var candidateFlag = FindByName(candidate);
                if (flag.ValueType == FlagValueType.Boolean
                    && candidateFlag?.ValueType == FlagValueType.Boolean)
                    pairs[positive] = candidate;
            }
            else
            {
                var negated = "--no-" + positive[2..];
                var negatedFlag = FindByName(negated);
                if (flag.ValueType == FlagValueType.Boolean
                    && negatedFlag?.ValueType == FlagValueType.Boolean)
                    pairs[positive] = negated;
            }
        }
        return pairs;
    }

    public static string? FindNegatedName(string flagName)
        => _negatedPairs.TryGetValue(flagName, out var negated) ? negated : null;
}
