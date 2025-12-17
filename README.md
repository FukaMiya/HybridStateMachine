## 概要
HybridStateMachineはUnity向けの軽量かつ柔軟なステートマシンライブラリです。
「イベント駆動（Push型）」と「条件監視（Pull型）」をシームレスに統合し、メソッドチェーンによる直感的な記述と、DIへの柔軟な対応を特徴としています。

## 特徴
- ハイブリッド駆動: `Update()`による条件監視（Pull）と、`Fire()`によるイベント通知（Push）を1つのステートマシン内で混在可能。イベントはEnumを使用して型安全に管理できます。

- Fluent Interface: `idle.To(jump).On(Event.Jump).When(() => IsGrounded).Build();` のように自然言語に近い形で遷移ロジックを記述できます。

- DI・Factory対応: 依存関係を持つステートの生成を制御でき、VContainerなどのDIコンテナや手動DIとスムーズに連携できます。

- Mermaid可視化: 構築したステートマシンをMermaid記法で出力し、遷移図として可視化可能です。

## 使い方
リポジトリ内の`Assets/StateMachine`フォルダを、Unityプロジェクトの任意の場所に配置してください。
使用時には`using HybridStateMachine;`を追記してください。
具体的な使い方は[GameEntryPoint.cs](Assets/GameEntryPoint.cs)を確認してください。Demo.unityから実行することができます。
このライブラリについての記事を書きました。[【Unity/C#】HybridでFluentなステートマシンを作った](https://zenn.dev/holybloodfilled/articles/28addda1f62b9b)

## ライセンス
[MIT License](LICENSE)