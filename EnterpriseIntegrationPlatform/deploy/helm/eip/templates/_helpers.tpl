{{/*
Expand the name of the chart.
*/}}
{{- define "eip.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "eip.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "eip.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "eip.labels" -}}
helm.sh/chart: {{ include "eip.chart" . }}
{{ include "eip.selectorLabels" . }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "eip.selectorLabels" -}}
app.kubernetes.io/name: {{ include "eip.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Component labels - takes a dict with Root (context) and Component (string)
*/}}
{{- define "eip.componentLabels" -}}
helm.sh/chart: {{ include "eip.chart" .Root }}
app.kubernetes.io/name: {{ .Component }}
app.kubernetes.io/instance: {{ .Root.Release.Name }}
app.kubernetes.io/version: {{ .Root.Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Root.Release.Service }}
app.kubernetes.io/component: {{ .Component }}
app.kubernetes.io/part-of: {{ include "eip.name" .Root }}
{{- end }}

{{/*
Component selector labels
*/}}
{{- define "eip.componentSelectorLabels" -}}
app.kubernetes.io/name: {{ .Component }}
app.kubernetes.io/instance: {{ .Root.Release.Name }}
{{- end }}

{{/*
Namespace
*/}}
{{- define "eip.namespace" -}}
{{- default "eip" .Values.global.namespace }}
{{- end }}

{{/*
Image pull secrets
*/}}
{{- define "eip.imagePullSecrets" -}}
{{- if .Values.global.imagePullSecrets }}
imagePullSecrets:
{{- range .Values.global.imagePullSecrets }}
  - name: {{ . }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Full image reference for a service
*/}}
{{- define "eip.image" -}}
{{- if .registry }}
{{- printf "%s/%s:%s" .registry .repository .tag }}
{{- else }}
{{- printf "%s:%s" .repository .tag }}
{{- end }}
{{- end }}
