// Tactical Map - Drag and Drop via event delegation
window.tacticalMap = {
    _dotNetRef: null,
    _isMaster: false,
    _draggingTokenId: null,
    _initialized: false,

    init: function (dotNetRef, isMaster) {
        this._dotNetRef = dotNetRef;
        this._isMaster = isMaster;

        if (this._initialized) return;
        this._initialized = true;

        // Use document-level delegation so it survives Blazor re-renders
        document.addEventListener('dragstart', (e) => {
            if (!this._isMaster) return;
            const token = e.target.closest('.map-token');
            if (!token) return;

            this._draggingTokenId = token.dataset.tokenId;
            e.dataTransfer.setData('text/plain', token.dataset.tokenId);
            e.dataTransfer.effectAllowed = 'move';

            // Slight delay to allow the drag image to capture before adding class
            setTimeout(() => token.classList.add('dragging'), 0);
        });

        document.addEventListener('dragover', (e) => {
            if (!this._isMaster || !this._draggingTokenId) return;
            const cell = e.target.closest('.grid-cell');
            if (!cell) return;
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
        });

        document.addEventListener('drop', (e) => {
            if (!this._isMaster || !this._draggingTokenId) return;

            const cell = e.target.closest('.grid-cell');
            if (!cell) return;

            e.preventDefault();
            e.stopPropagation();

            const x = parseInt(cell.dataset.x);
            const y = parseInt(cell.dataset.y);
            const tokenId = this._draggingTokenId;
            this._draggingTokenId = null;

            this.wasDrop = true;
            if (this._dotNetRef) {
                this._dotNetRef.invokeMethodAsync('OnTokenDropped', tokenId, x, y);
            }
        });

        document.addEventListener('dragend', (e) => {
            this._draggingTokenId = null;
            const token = e.target.closest('.map-token');
            if (token) token.classList.remove('dragging');
        });
    },

    // Called by Blazor to check if a drop just happened (to skip onclick)
    wasDrop: false,
    checkAndResetDrop: function () {
        var was = this.wasDrop;
        this.wasDrop = false;
        return was;
    },

    dispose: function () {
        this._dotNetRef = null;
        this._isMaster = false;
        this._draggingTokenId = null;
        this._initialized = false;
    }
};
