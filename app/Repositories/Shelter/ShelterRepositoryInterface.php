<?php

namespace App\Repositories\Shelter;

use App\Models\Shelter as ShelterModel;
use Illuminate\Database\Eloquent\Collection;
use Illuminate\Database\Eloquent\Model;

interface ShelterRepositoryInterface
{
    public function getById($id): ShelterModel|Collection|Model|null;

    public function getAll(): Collection;

    public function create(array $data): ShelterModel;

    public function update($id, array $data): ShelterModel|Model|null;
}
