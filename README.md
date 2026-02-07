# BS-CameraMovement

このBeatSaberプラグインは、公式譜面エディタ にカメラワーク機能を追加するプラグインです。

外部スクリプトファイル (`SongScript.json`) を読み込むことで、楽曲の再生に合わせてエディタ上のカメラを自動で動かすことができます。

<img width="1282" height="752" alt="image" src="https://github.com/user-attachments/assets/20bc2010-1bc3-4173-875e-6acc998b6a9a" />

## 機能

*   譜面フォルダ内の `SongScript.json` に基づき、エディタ上のメインカメラを制御します。
*   エディタ起動中に `SongScript.json` を編集・保存すると、自動的に検知して再読み込みを行い、変更を即座に反映します。
*   チェックボックスでBS-CameraMovementの機能の有効/無効を切り替えられます。無効にするとカメラはデフォルトの位置,FOV,画面サイズに戻ります。
*   現在のカメラの位置、回転、視野角（FOV）をリアルタイムに表示・編集できます。
*   カメラの座標データをクリップボード経由でコピー＆ペーストできます（ScriptMapperの `q_` フォーマットにも対応）。

## インストール

1.  [リリースページ](https://github.com/rynan4818/BS-CameraMovement/releases)から`BS-CameraMovement`のリリースをダウンロードします。
2.  ダウンロードしたzipファイルを`Beat Saber`フォルダに解凍して、`Plugins` フォルダに`BS-CameraMovement.dll`を配置してください。
    *   前提として`BSIPA`と`SiraUtil`が導入されている必要があります。

## 使い方

1.  公式エディタはブックマークや`SongScript.json`などの非公式なデータを削除してしまうことがあるため、今まで通りカメラスクリプト作成は`CustomWIPLevels`フォルダに置いた譜面をChroMapperで作成します。
2. `CustomLevels`フォルダに、`CustomWIPLevels`で編集中の譜面フォルダをコピーします。（同名のフォルダ名を作成します）
3. ScriptMapperは`CustomLevels`フォルダに同名のフォルダがあると、そちらにも `SongScript.json`をコピーしてくれます。
4. Beat Saber エディタで`CustomLevels`フォルダのマップを開きます。(公式エディタは`CustomLevels`フォルダの譜面しか表示できないと思います)
5. 画面上の "Show Camera UI" ボタンを押してウィンドウを表示し、"Enable CameraMovement" にチェックが入っていることを確認します。
6. タイムラインを再生すると、スクリプトに従ってカメラが動きます。
7. ScriptMapperで`SongScript.json`を更新すると、BS-CameraMovementが自動で検知してカメラスクリプトを読み込み直しします。

### UI 操作
<img width="319" height="298" alt="image" src="https://github.com/user-attachments/assets/d25c7326-a083-4208-b4f7-580f93e714c4" />

*   **Show/Hide Camera UI**: 画面右上のボタンで設定ウィンドウの表示/非表示を切り替えます。
*   **Enable CameraMovement**: 機能のON/OFFを切り替えます。OFFにすると通常のカメラ操作に戻ります。
*   **Copy / Paste**:
    *   **Copy**: 現在のカメラ情報をテキストとしてクリップボードにコピーします。
    *   **Paste**: クリップボードのテキスト情報を読み取り、カメラに適用します。
    *   **q_format**: チェックを入れると、コピー時のフォーマットが `q_x_y_z_rx_ry_rz_fov` 形式になります（チェックを外すとタブ区切り形式）。
