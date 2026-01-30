import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { type OcrSummary } from "./dashboard";
import { Loader2 } from "lucide-react";
import { Tooltip, TooltipTrigger, TooltipContent } from "@/components/ui/tooltip";

export default function OcrSummaryCard({
  summary,
  loading,
}: {
  summary: OcrSummary | null;
  loading: boolean;
}) {
  if (!summary && !loading) return null;

  return (
    <Card>
      <CardHeader>
        <CardTitle>OCR Summary</CardTitle>
      </CardHeader>

      <CardContent className="space-y-3">
        {loading ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading OCR status…
          </div>
        ) : (
          <div className="grid grid-cols-2 gap-3 text-sm">
            <Stat label="Total" value={summary?.total ?? 0} />
            <Stat label="Completed" value={summary?.completed ?? 0} />
            <Stat label="Processing" value={summary?.processing ?? 0} />
            <Tooltip>
              <TooltipTrigger asChild>
                <div>
                  <Stat label="Failed" value={summary?.failed ?? 0} error />
                </div>
              </TooltipTrigger>
              <TooltipContent>
                Failed OCR tasks
              </TooltipContent>
            </Tooltip>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function Stat({
  label,
  value,
  error,
}: {
  label: string;
  value: number;
  error?: boolean;
}) {
  return (
    <div className="flex justify-between">
      <span className="text-muted-foreground">{label}</span>
      <span className={error ? "text-red-500 font-medium" : "font-medium"}>
        {value}
      </span>
    </div>
  );
}
